"""Run against the local preview with TEMPLE_TEST_PASSWORD configured."""
import html
import http.cookiejar
import os
import re
import urllib.error
import urllib.parse
import urllib.request

base = os.environ.get("TEMPLE_TEST_URL", "http://127.0.0.1:5209")
path = "/Identity/Account/Login"
def client():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def submit(browser, values, target=path):
    with browser.open(base + target) as r:
        page = r.read().decode()
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
    values = dict(values, __RequestVerificationToken=html.unescape(token))
    with browser.open(base + target, urllib.parse.urlencode(values).encode()) as r:
        return r.url, html.unescape(r.read().decode())
def check(ok, message):
    if not ok: raise AssertionError(message)
    print("PASS: " + message)

b = client()
with b.open(base + path) as r:
    page = html.unescape(r.read().decode())
check('lang="bn"' in page and 'login.css' in page and 'toggle-password' in page, "Custom Bengali login page renders")
url, page = submit(b, {"Input.Email": "", "Input.Password": ""})
check("ইমেইল ঠিকানা লিখুন।" in page and "পাসওয়ার্ড লিখুন।" in page, "Empty fields have Bengali server validation")
url, page = submit(b, {"Input.Email": "unknown-login-check@example.com", "Input.Password": "Invalid123!"})
check("প্রবেশ করা যায়নি।" in page and path in url, "Invalid credentials remain on login with Bengali error")
credentials = {"Input.Email": os.environ.get("TEMPLE_TEST_EMAIL", "admin@doyamoyeemondir.local"), "Input.Password": os.environ["TEMPLE_TEST_PASSWORD"]}
url, _ = submit(b, credentials, path + "?ReturnUrl=%2FEmployees")
check(url.endswith("/Employees"), "Successful login preserves local return URL")
url, _ = submit(client(), credentials, path + "?ReturnUrl=https%3A%2F%2Fexample.com")
check(url == base + "/", "External return URL safely falls back to home")
try:
    client().open(base + path, urllib.parse.urlencode(credentials).encode())
    raise AssertionError("Missing antiforgery token accepted")
except urllib.error.HTTPError as e:
    check(e.code == 400, "Login POST requires antiforgery token")
