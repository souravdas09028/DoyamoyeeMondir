"""Local preview only; runs prasad fixture then refund and bank-close checks."""
from http_prasad_smoke import get, post, check, field, original, product, today, suffix, values
import html, re

receipt_id=original.rsplit('/',1)[1]
source='/Corrections/Reverse?kind=1&id='+receipt_id
_,page=get(source)
refund={'SourceKind':1,'SourceId':receipt_id,'Date':today,'Reason':'পরীক্ষার সম্পূর্ণ প্রসাদ ফেরত','Version':field(page,'Version'),'SubmissionKey':field(page,'SubmissionKey')}
url,_=post('/Corrections/Reverse',refund,source)
check('/Corrections/Reverse' not in url,'Refund form saves correction')
_,page=get('/Income/Receipt/'+receipt_id)
check('সংশোধিত / ফেরত' in html.unescape(page),'Original receipt clearly marks refund')
_,page=get('/Prasad/Sell/'+product)
check('মজুত: 10' in html.unescape(page),'Full prasad refund restores stock')
# Replay the identical POST with a fresh antiforgery token from any correction form.
url,_=post('/Corrections/Reverse',refund,'/Reconciliation/Create')
check('/Corrections/Reverse' not in url,'Repeated refund is idempotent')
_,page=get('/Prasad/Sell/'+product)
check('মজুত: 10' in html.unescape(page),'Repeated refund does not restore stock twice')
bank_name='সমন্বয় পরীক্ষা '+suffix
post('/Settings/Edit',{'Id':0,'Kind':'account','NameBn':bank_name,'Code':'REC'+suffix,'Amount':50,'OpeningDate':'2026-01-01','IsCashAccount':'false','IsActive':'true'},'/Settings/Edit?kind=account')
_,page=get('/Reconciliation/Create')
bank=next(v for v,label in re.findall(r'<option value="(\d+)">(.*?)</option>',html.unescape(page)) if bank_name in label)
form={'AccountId':bank,'Date':today,'StatementBalance':51,'Reference':'পরীক্ষার অমিল','Close':'true'}
_,page=post('/Reconciliation/Create',form)
check('অমিল রয়েছে' in html.unescape(page),'Mismatched statement cannot close account')
form['Close']='false'
url,page=post('/Reconciliation/Create',form)
check('/Reconciliation/Create' not in url and 'পরীক্ষার অমিল' in html.unescape(page),'Unmatched comparison can be recorded')
form.update(StatementBalance=50,Close='true',Reference='পরীক্ষার মিল')
url,page=post('/Reconciliation/Create',form)
check('/Reconciliation/Create' not in url and 'যাচাইকৃত ও বন্ধ' in html.unescape(page),'Matched statement closes account')
url,page=get('/Reconciliation/Ledger/'+bank)
check('50.00' in page and today in page,'Bank ledger displays balance and closure')
_,page=post('/Income/Create',{'IncomeCategoryId':values['IncomeCategoryId'],'CashBankAccountId':bank,'Date':today,'Amount':1,'Description':'বন্ধ সময়ে পরীক্ষা','PaymentMethod':1})
check('validation-summary-errors' in page,'Closed period rejects new income through MVC')
