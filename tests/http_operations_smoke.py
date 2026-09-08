"""Run after starting the local preview; also executes the base HTTP smoke suite."""
from http_smoke import *
import urllib.error

for route in ("/Accounts", "/Accounts/Transfer", "/Accounts/Transfers", "/Accounts/Payments", "/Inventory", "/Inventory/Edit", "/Assets", "/Assets/Edit", "/Committees", "/Committees/Edit", "/Documents", "/Documents/Upload"):
    url, page = get(route)
    check("/Identity/" not in url and '<html lang="bn"' in page, "Operations page renders: " + route)

def option_id(page, name, text):
    select = re.search(r'<select\b[^>]*name="' + name + r'"[^>]*>(.*?)</select>', page, re.S).group(1)
    for value, label in re.findall(r'<option value="(\d+)"[^>]*>(.*?)</option>', select, re.S):
        if text in html.unescape(label): return value
    raise AssertionError("Choice not found: " + text)

post("/Settings/Edit", {"Id": "0", "Kind": "expense", "NameBn": "অনুমোদিত খরচ " + suffix, "Code": "E" + suffix, "RequiresApproval": "true", "IsActive": "true"}, "/Settings/Edit?kind=expense")
_, page = get("/Expenses/Create")
expense_category = option_id(page, "ExpenseCategoryId", suffix)
url, page = post("/Expenses/Create", {"ExpenseCategoryId": expense_category, "Date": "2026-09-08", "Amount": "30", "Description": "পরীক্ষার ব্যয় " + suffix})
expense_id = field(page, "id")
version = field(page, "rowVersion")
url, page = post("/Expenses/Review", {"id": expense_id, "approve": "true", "rowVersion": version, "note": "অনুমোদিত"}, "/Expenses")
check("/Expenses/Review" not in url, "Expense approval succeeds through form")
_, page = get("/Accounts/Pay/" + expense_id)
cash_id = option_id(page, "CashBankAccountId", suffix)
url, page = post("/Accounts/Pay", {"ExpenseId": expense_id, "CashBankAccountId": cash_id, "Payee": "পরীক্ষার প্রাপক", "Date": "2026-09-08", "Amount": "10", "PaymentMethod": "1"}, "/Accounts/Pay/" + expense_id)
check("/Accounts/Voucher" in url and "10.00" in page, "Partial payment opens Bengali voucher")
url, page = post("/Accounts/Pay", {"ExpenseId": expense_id, "CashBankAccountId": cash_id, "Payee": "পরীক্ষার প্রাপক", "Date": "2026-09-08", "Amount": "21", "PaymentMethod": "1"}, "/Accounts/Pay/" + expense_id)
check("বকেয়া" in html.unescape(page) and "/Accounts/Voucher" not in url, "Overpayment is rejected by live form")

post("/Settings/Edit", {"Id": "0", "Kind": "account", "NameBn": "ব্যাংক " + suffix, "Code": "B" + suffix, "Amount": "0", "OpeningDate": "2026-01-01", "IsCashAccount": "false", "IsActive": "true"}, "/Settings/Edit?kind=account")
_, page = get("/Accounts/Transfer")
bank_id = option_id(page, "ToAccountId", "ব্যাংক " + suffix)
url, page = post("/Accounts/Transfer", {"FromAccountId": cash_id, "ToAccountId": bank_id, "Amount": "5", "Date": "2026-09-08", "Description": "স্থানান্তর " + suffix})
check("/Accounts/Transfers" in url, "Account transfer is recorded")
_, page = get("/Accounts?asOf=2026-09-08")
check("25.00" in page and "5.00" in page, "Balance reflects collection, payment and transfer")

url, page = post("/Inventory/Edit", {"Id": "0", "NameBn": "চাল " + suffix, "Unit": "কেজি", "ReorderLevel": "2", "IsActive": "true"})
check("/Inventory/Edit" not in url, "Inventory item saves")
_, page = get("/Inventory?q=" + suffix)
move_path = html.unescape(re.findall(r'href="([^"]*/Inventory/Move[^"]*)"', page)[0])
item_id = re.search(r"/Move/(\d+)", move_path).group(1)
url, page = post("/Inventory/Move", {"InventoryItemId": item_id, "IsReceipt": "true", "Quantity": "12.5", "Date": "2026-09-08", "Reason": "প্রারম্ভিক মজুত"}, move_path)
check("/Inventory/History" in url and "12.5" in page, "Stock receipt updates history and quantity")
url, page = post("/Inventory/Move", {"InventoryItemId": item_id, "IsReceipt": "false", "Quantity": "13", "Date": "2026-09-08", "Reason": "অতিরিক্ত ব্যবহার"}, "/Inventory/Move/" + item_id + "?receipt=false")
check("পর্যাপ্ত মজুত নেই" in html.unescape(page), "Stock over-issue is rejected")
url, page = post("/Inventory/Move", {"InventoryItemId": item_id, "IsReceipt": "false", "Quantity": "2.5", "Date": "2026-09-08", "Reason": "প্রসাদ রান্না"}, "/Inventory/Move/" + item_id + "?receipt=false")
check("/Inventory/History" in url and "বর্তমান: 10" in html.unescape(page), "Stock usage reduces balance")

url, page = post("/Assets/Edit", {"Id": "0", "NameBn": "অলংকার " + suffix, "Code": "AST" + suffix, "Description": "পরীক্ষার রৌপ্য অলংকার", "Location": "সংরক্ষণ কক্ষ", "Custodian": "দায়িত্বপ্রাপ্ত", "Material": "রৌপ্য", "WeightGrams": "12.5", "EstimatedValue": "1000", "ReceivedDate": "2026-09-08", "IsActive": "true"})
check("/Assets/Edit" not in url and "AST" + suffix in page, "Asset and ornament record saves")
url, page = post("/Committees/Edit", {"Id": "0", "NameBn": "কমিটি " + suffix, "StartDate": "2026-01-01", "EndDate": "2026-12-31"})
check("/Committees/Members" in url, "Committee term saves")
committee_id = field(page, "CommitteeId")
member_person = selected_id(page, "PersonId")
url, page = post("/Committees/Members", {"CommitteeId": committee_id, "PersonId": member_person, "Position": "সভাপতি"}, "/Committees/Members/" + committee_id)
check("সভাপতি" in html.unescape(page), "Committee member assignment saves")

def upload_document(content, filename):
    _, page = get("/Documents/Upload")
    boundary = "----TempleTest" + uuid.uuid4().hex
    parts = []
    for name, value in {"Title": "নথি " + suffix, "Reference": "DOC" + suffix, "Date": "2026-09-08", "__RequestVerificationToken": field(page, "__RequestVerificationToken")}.items():
        parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n').encode())
    parts.append((f'--{boundary}\r\nContent-Disposition: form-data; name="Upload"; filename="{filename}"\r\nContent-Type: application/pdf\r\n\r\n').encode() + content + b"\r\n")
    parts.append((f"--{boundary}--\r\n").encode())
    request = urllib.request.Request(base + "/Documents/Upload", data=b"".join(parts), headers={"Content-Type": "multipart/form-data; boundary=" + boundary})
    with client.open(request) as response: return response.geturl(), response.read().decode()

url, page = upload_document(b"<html>unsupported</html>", "bad.pdf")
check("সমর্থিত" in html.unescape(page) and "/Documents/Upload" in url, "Mislabeled document is rejected")
content = b"%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\n%%EOF\n"
url, page = upload_document(content, "test.pdf")
check("/Documents/Upload" not in url, "Document upload is saved")
_, page = get("/Documents?q=" + suffix)
download = html.unescape(re.findall(r'href="([^"]*/Documents/Download[^"]*)"', page)[0])
with client.open(base + download) as response:
    check(response.read() == content and "attachment" in response.headers["Content-Disposition"], "Authorized download preserves exact content as attachment")
anonymous = urllib.request.build_opener()
with anonymous.open(base + download) as response:
    check("/Identity/Account/Login" in response.geturl(), "Anonymous document download requires login")
try:
    client.open(urllib.request.Request(base + "/Accounts/Transfer", data=b"Amount=1"))
    raise AssertionError("Missing antiforgery token accepted")
except urllib.error.HTTPError as error:
    check(error.code == 400, "Financial POST requires antiforgery token")
print("PASS: Operations HTTP smoke checks completed")
