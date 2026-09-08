"""Prasad checks against the separate local preview; retains named test records."""
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
import datetime
suffix=uuid.uuid4().hex[:8]
name="প্রসাদ পরীক্ষা "+suffix
today=datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=6))).date().isoformat()
post("/Identity/Account/Login", {"Input.Email":"admin@doyamoyeemondir.local", "Input.Password":os.environ["TEMPLE_TEST_PASSWORD"]})
_, page=post("/Inventory/Edit", {"Id":0,"NameBn":name,"Unit":"প্যাকেট","ReorderLevel":2,"IsActive":"true"})
row=next(r for r in re.findall(r"<tr>.*?</tr>",html.unescape(page),re.S) if name in r)
item=re.search(r'/Inventory/Edit/(\d+)',row).group(1)
post("/Inventory/Move", {"InventoryItemId":item,"Quantity":10,"IsReceipt":"true","Reason":"প্রসাদ পরীক্ষার মজুত","Date":today},"/Inventory/Move/"+item)
url,page=post("/Prasad/Edit",{"Id":0,"NameBn":name,"InventoryItemId":item,"Price":25,"IsActive":"true"})
check("/Prasad" in url and name in html.unescape(page),"Product form saves Bengali product")
row=next(r for r in re.findall(r"<tr>.*?</tr>",html.unescape(page),re.S) if name in r)
product=re.search(r'/Prasad/Sell/(\d+)',row).group(1)
for kind in ("income","account"):
    values={"Id":0,"Kind":kind,"NameBn":name,"Code":("PI" if kind=="income" else "PA")+suffix,"IsActive":"true"}
    if kind=="account": values.update(Amount=0,OpeningDate="2026-01-01",IsCashAccount="true")
    post("/Settings/Edit",values,"/Settings/Edit?kind="+kind)
_,page=get("/Prasad/Sell/"+product)
def option(name_field):
    select=re.search(r'<select[^>]*name="'+name_field+r'"[^>]*>(.*?)</select>',page,re.S).group(1)
    return next(v for v,label in re.findall(r'<option value="(\d+)">(.*?)</option>',html.unescape(select)) if name in label)
values={"ProductId":product,"ProductVersion":field(page,"ProductVersion"),"SubmissionKey":field(page,"SubmissionKey"),"Date":today,"Quantity":3,"IncomeCategoryId":option("IncomeCategoryId"),"CashBankAccountId":option("CashBankAccountId"),"PaymentMethod":1}
url,page=post("/Prasad/Sell",values,"/Prasad/Sell/"+product)
check("/Income/Receipt/" in url and "75.00" in page,"Prasad sale returns correct receipt")
original=url
url,_=post("/Prasad/Sell",values,"/Prasad/Sell/"+product)
check(url==original,"Repeated sale returns original receipt")
_,page=get("/Prasad/Sell/"+product)
check("মজুত: 7" in html.unescape(page),"Sale reduces stock once")
values.update(SubmissionKey=field(page,"SubmissionKey"),ProductVersion=field(page,"ProductVersion"),Quantity=8)
_,page=post("/Prasad/Sell",values,"/Prasad/Sell/"+product)
check("পর্যাপ্ত মজুত নেই" in html.unescape(page),"Overselling rejected by actual MVC form")



