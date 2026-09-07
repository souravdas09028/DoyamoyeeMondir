"""Live form checks against the separate local preview database.
Run with TEMPLE_TEST_PASSWORD set to the preview administrator password.
"""
import html
import http.cookiejar
import os
import re
import urllib.parse
import urllib.request

base = os.environ.get("TEMPLE_TEST_URL", "http://127.0.0.1:5209")
client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

def get(path):
    with client.open(base + path) as response:
        return response.geturl(), response.read().decode("utf-8")

def field(page, name):
    for element in re.findall(r"<input\b[^>]*>", page):
        attrs = dict(re.findall(r'([\w-]+)="([^"]*)"', element))
        if attrs.get("name") == name:
            return html.unescape(attrs.get("value", ""))
    raise AssertionError("Missing form field: " + name)

def post(path, values, source=None):
    _, page = get(source or path)
    values = dict(values)
    values["__RequestVerificationToken"] = field(page, "__RequestVerificationToken")
    for name in ("SubmissionKey", "RowVersion", "ServiceVersion"):
        if f'name="{name}"' in page and name not in values:
            values[name] = field(page, name)
    request = urllib.request.Request(base + path, data=urllib.parse.urlencode(values).encode())
    with client.open(request) as response:
        return response.geturl(), response.read().decode("utf-8")

def check(condition, message):
    if not condition:
        raise AssertionError(message)
    print("PASS: " + message)

url, _ = get("/Income")
check("/Identity/Account/Login" in url, "Anonymous income access redirects to login")
url, page = post("/Identity/Account/Login", {"Input.Email": "admin@doyamoyeemondir.local", "Input.Password": os.environ["TEMPLE_TEST_PASSWORD"], "Input.RememberMe": "false"})
check("/Identity/Account/Login" not in url, "Administrator login succeeds")

for route in ("/", "/People", "/People/Edit", "/Memberships", "/Memberships/Create", "/Income", "/Income/Create", "/Income/Services", "/Expenses", "/Reports"):
    url, page = get(route)
    check("/Identity/" not in url and '<html lang="bn"' in page, "Bengali page renders: " + route)

import uuid
suffix = uuid.uuid4().hex[:8]
for kind, extra in (("income", {"Code": "I" + suffix}), ("account", {"Code": "A" + suffix, "Amount": "0", "OpeningDate": "2026-01-01", "IsCashAccount": "true"}), ("membership", {"Amount": "100"}), ("service", {"Amount": "100", "TempleShare": "60", "PriestShare": "30", "StaffShare": "10"})):
    url, page = post("/Settings/Edit", {"Id": "0", "Kind": kind, "NameBn": "পরীক্ষা " + suffix, "IsActive": "true", **extra}, "/Settings/Edit?kind=" + kind)
    check("/Settings?" in url or "/Settings/Index" in url, "Create settings through actual MVC form: " + kind)

url, page = post("/People/Edit", {"Id": "0", "NameBn": "পরীক্ষার ভক্ত " + suffix, "MobileNumber": "01700000000", "AddressBn": "ঢাকা", "IsActive": "true"})
check(url.rstrip("/").endswith("People") or "/People/Index" in url, "Devotee form saves Bengali data")
check("পরীক্ষার ভক্ত" in html.unescape(page), "Saved Bengali name renders correctly")

_, page = get("/Memberships/Create")
def selected_id(page, name):
    select = re.search(r'<select\b[^>]*name="' + name + r'"[^>]*>(.*?)</select>', page, re.S).group(1)
    return re.findall(r'<option value="(\d+)"', select)[-1]
person_id = selected_id(page, "PersonId")
type_id = selected_id(page, "MembershipTypeId")
url, page = post("/Memberships/Create", {"PersonId": person_id, "MembershipTypeId": type_id, "StartDate": "2026-01-01", "EndDate": "2026-12-31"})
check("/Memberships/Create" not in url, "Membership enrollment form saves")
collection_path = html.unescape(re.findall(r'href="([^"]*membershipId=\d+[^"]*)"', page)[0])
membership_id = urllib.parse.parse_qs(urllib.parse.urlparse(collection_path).query)["membershipId"][0]
_, page = get(collection_path)
category_id = selected_id(page, "IncomeCategoryId")
account_id = selected_id(page, "CashBankAccountId")
url, page = post("/Income/Create", {"MembershipId": membership_id, "PersonId": person_id, "Date": "2026-09-08", "Amount": "40", "IncomeCategoryId": category_id, "CashBankAccountId": account_id, "Description": "পরীক্ষার চাঁদা", "PaymentMethod": "1"}, collection_path)
check("/Income/Receipt/" in url or "/Income/Receipt?" in url, "Collection POST opens receipt")
check("40.00" in page and "অর্থ গ্রহণের রসিদ" in html.unescape(page), "Receipt renders amount and Bengali heading")
url, page = get("/Reports?from=2026-09-01&to=2026-09-30")
check("40.00" in page, "Report includes saved collection")
print("PASS: Live HTTP smoke checks completed (preview data retained)")
