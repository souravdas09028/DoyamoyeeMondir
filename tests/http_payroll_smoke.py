"""Payroll form checks; uses the isolated preview and retains named test records."""
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


import uuid
suffix = uuid.uuid4().hex[:8]
post("/Identity/Account/Login", {"Input.Email": "admin@doyamoyeemondir.local", "Input.Password": os.environ["TEMPLE_TEST_PASSWORD"], "Input.RememberMe": "false"})
import re
import datetime
import html

today = datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=6))).date()
month = today.replace(day=1).isoformat()
name = "বেতন পরীক্ষা " + suffix
url, page = post("/Employees/Edit", {"Id": 0, "NameBn": name, "Position": "কর্মী", "MonthlySalary": "100", "JoinedOn": "2026-01-01", "IsActive": "true"})
check(url.endswith("/Employees") or "/Employees/Index" in url, "Employee creation accepts the actual MVC form")
row = next(r for r in re.findall(r"<tr>.*?</tr>", html.unescape(page), re.S) if name in r)
employee = re.search(r'/Employees/Generate/(\d+)', row).group(1)
url, page = post("/Settings/Edit", {"Id": 0, "Kind": "expense", "NameBn": name, "Code": "PAY"+suffix, "RequiresApproval": "true", "IsActive": "true"}, "/Settings/Edit?kind=expense")
_, page = get("/Employees/Generate/" + employee)
category = next(value for value, label in re.findall(r'<option value="(\d+)">(.*?)</option>', html.unescape(page)) if name in label)
values = {"EmployeeId": employee, "EmployeeVersion": field(page, "EmployeeVersion"), "SubmissionKey": field(page, "SubmissionKey"), "Month": month, "Date": today.isoformat(), "ExpenseCategoryId": category, "Allowance": "20", "Deduction": "5"}
url, page = post("/Employees/Generate", values, "/Employees/Generate/"+employee)
check("/Employees/Payroll" in url and name in html.unescape(page) and "115.00" in page, "Payroll form creates correct salary expense")
check("অনুমোদনের অপেক্ষায়" in html.unescape(page), "Salary expense follows configured approval rule")
url, page = post("/Employees/Generate", values, "/Employees/Generate/"+employee)
check(html.unescape(page).count(name) == 1, "Repeated salary submission creates one payroll row")
_, form = get("/Employees/Generate/"+employee)
values["SubmissionKey"] = field(form, "SubmissionKey")
values["EmployeeVersion"] = field(form, "EmployeeVersion")
url, page = post("/Employees/Generate", values, "/Employees/Generate/"+employee)
check("ইতিমধ্যে নথিভুক্ত" in html.unescape(page), "Fresh submission rejects duplicate month")

