"""User-administration acceptance checks against local preview. Retains inactive test users."""
import html, http.cookiejar, os, re, urllib.request, urllib.parse, urllib.error, uuid
base=os.environ.get("TEMPLE_TEST_URL","http://127.0.0.1:5209")
def browser(): return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def get(b,path):
    with b.open(base+path) as r: return r.url,html.unescape(r.read().decode())
def field(page,name):
    for tag in re.findall(r'<input\b[^>]*>',page):
        attrs=dict(re.findall(r'([\w-]+)="([^"]*)"',tag))
        if attrs.get('name')==name: return attrs.get('value','')
    raise AssertionError('Missing '+name)
def post(b,path,values,source=None):
    _,p=get(b,source or path)
    data=dict(values,__RequestVerificationToken=field(p,'__RequestVerificationToken'))
    for key in ('Stamp',):
        if 'name="'+key+'"' in p and key not in data: data[key]=field(p,key)
    with b.open(base+path,urllib.parse.urlencode(data,doseq=True).encode()) as r: return r.url,html.unescape(r.read().decode())
def login(b,email,password): return post(b,'/Identity/Account/Login',{'Input.Email':email,'Input.Password':password})
def check(ok,message):
    if not ok: raise AssertionError(message)
    print('PASS: '+message)
admin=browser()
login(admin,os.environ.get('TEMPLE_TEST_EMAIL','admin@doyamoyeemondir.local'),os.environ['TEMPLE_TEST_PASSWORD'])
suffix=uuid.uuid4().hex[:10]; email='user-check-'+suffix+'@example.com'; password='CheckA1!'+uuid.uuid4().hex
url,p=post(admin,'/Users/Edit',{'NameBn':'ব্যবহারকারী পরীক্ষা '+suffix,'Email':email,'Password':password,'Roles':['Viewer'],'IsActive':'true'})
check('/Users/Edit' not in url and email in p,'Admin creates user with a role')
row=next(r for r in re.findall(r'<tr>.*?</tr>',p,re.S) if email in r)
uid=re.search(r'/Users/Edit/([^"?]+)',row).group(1)
edit='/Users/Edit/'+uid
u=browser(); login(u,email,password)
url,_=get(u,'/Users')
check('AccessDenied' in url,'Viewer cannot access administration')
try:
    get(browser(),'/Identity/Account/Register'); raise AssertionError('Registration available')
except urllib.error.HTTPError as e: check(e.code==404,'Public registration disabled')
_,p=get(admin,edit); stamp=field(p,'Stamp')
values={'Id':uid,'Stamp':stamp,'NameBn':'ব্যবহারকারী পরীক্ষা '+suffix,'Email':email,'Roles':['InventoryManager'],'IsActive':'true'}
url,_=post(admin,'/Users/Edit',values,edit)
check('/Users/Edit' not in url,'Administrator changes role')
url,_=get(u,'/Inventory')
check('/Identity/Account/Login' in url,'Role change invalidates existing login')
login(u,email,password); url,_=get(u,'/Inventory')
check('/Identity/' not in url,'New role grants intended module')
_,p=post(admin,'/Users/Edit',values,edit)
check('তথ্য পরিবর্তিত হয়েছে' in p,'Stale user form rejected')
url,p=post(admin,'/Users/Edit',{'NameBn':'duplicate','Email':email,'Password':password,'Roles':['SuperAdmin'],'IsActive':'true'})
check('ইতিমধ্যে ব্যবহৃত' in p,'Duplicate email rejected without role escalation')
url,_=get(u,'/Users'); check('AccessDenied' in url,'Failed duplicate create does not grant admin access')
_,p=get(admin,edit); values['Stamp']=field(p,'Stamp'); values['IsActive']='false'
post(admin,'/Users/Edit',values,edit)
url,_=get(u,'/Inventory'); check('/Identity/Account/Login' in url,'Deactivation invalidates session')
url,p=login(browser(),email,password); check('/Identity/Account/Login' in url and 'প্রবেশ করা যায়নি' in p,'Inactive account cannot log in')
_,p=get(admin,edit); values['Stamp']=field(p,'Stamp'); values['IsActive']='true'
post(admin,'/Users/Edit',values,edit); login(u,email,password)
new_password='ResetA1!'+uuid.uuid4().hex
reset='/Users/ResetPassword/'+uid
url,_=post(admin,'/Users/ResetPassword',{'Id':uid,'Password':new_password,'ConfirmPassword':new_password},reset)
check('/Users/ResetPassword' not in url,'Admin password reset succeeds')
url,_=get(u,'/Inventory'); check('/Identity/Account/Login' in url,'Password reset revokes existing session')
url,_=login(browser(),email,password); check('/Identity/Account/Login' in url,'Old password no longer works')
url,_=login(browser(),email,new_password); check('/Identity/Account/Login' not in url,'New password works')
_,p=get(admin,'/Users/Audit'); check(email in p and password not in p and new_password not in p,'Audit identifies user without logging passwords')
# Self-demotion must be rejected, including when this is the only super administrator.
_,p=get(admin,'/Users?search='+urllib.parse.quote(os.environ.get('TEMPLE_TEST_EMAIL','admin@doyamoyeemondir.local')))
admin_id=re.search(r'/Users/Edit/([^"?]+)',p).group(1)
_,p=get(admin,'/Users/Edit/'+admin_id)
url,p=post(admin,'/Users/Edit',{'Id':admin_id,'Stamp':field(p,'Stamp'),'NameBn':field(p,'NameBn'),'Email':field(p,'Email'),'Roles':['Viewer'],'IsActive':'false'},'/Users/Edit/'+admin_id)
check('নিজের প্রধান প্রশাসকের' in p,'Self-deactivation and demotion rejected')
_,p=get(admin,edit); values['Stamp']=field(p,'Stamp'); values['IsActive']='false'
post(admin,'/Users/Edit',values,edit)
try:
    admin.open(base+'/Users/Edit',urllib.parse.urlencode(values,doseq=True).encode()); raise AssertionError('Antiforgery missing')
except urllib.error.HTTPError as e: check(e.code==400,'Administrative writes require antiforgery token')
