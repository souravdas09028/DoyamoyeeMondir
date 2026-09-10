from pathlib import Path
from copy import deepcopy
import json, zipfile, hashlib
from lxml import etree as E

root=Path(__file__).resolve().parent
reference=Path('C:/Users/MDS/.codex/plugins/cache/openai-curated-remote/openai-templates/0.1.1/skills/artifact-template-design-report/assets/reference.docx')
output=root.parent/'Doyamoyee-User-Manual-Bengali.docx'
pages=json.loads((root/'content.json').read_text(encoding='utf-8-sig'))
W='http://schemas.openxmlformats.org/wordprocessingml/2006/main'; ns={'w':W}
def tag(x): return '{'+W+'}'+x
with zipfile.ZipFile(reference) as z: parts={n:z.read(n) for n in z.namelist()}
doc=E.fromstring(parts['word/document.xml']); body=doc.find('w:body',ns)
children=list(body); section=deepcopy(children[-1]); normal=deepcopy(children[11]); heading=deepcopy(children[10])
def paragraph(text,size=24,bold=False,template=None,newpage=False,keep=False):
    p=deepcopy(template if template is not None else normal)
    for c in list(p):
        if c.tag!=tag('pPr'): p.remove(c)
    pr=p.find('w:pPr',ns)
    if pr is None: pr=E.SubElement(p,tag('pPr'))
    for key in ('numPr','ind','spacing','pageBreakBefore','keepNext','keepLines','pBdr','shd'):
        for e in pr.findall('w:'+key,ns): pr.remove(e)
    if newpage: E.SubElement(pr,tag('pageBreakBefore'))
    if keep: E.SubElement(pr,tag('keepNext'))
    spacing=E.SubElement(pr,tag('spacing')); spacing.set(tag('after'),'110'); spacing.set(tag('line'),'290'); spacing.set(tag('lineRule'),'auto')
    r=E.SubElement(p,tag('r')); rp=E.SubElement(r,tag('rPr'))
    fonts=E.SubElement(rp,tag('rFonts'))
    for k in ('ascii','hAnsi','cs','eastAsia'): fonts.set(tag(k),'Nirmala UI')
    for k in ('sz','szCs'): E.SubElement(rp,tag(k)).set(tag('val'),str(size))
    E.SubElement(rp,tag('color')).set(tag('val'),'000000')
    E.SubElement(rp,tag('lang')).set(tag('bidi'),'bn-BD')
    if bold:
        E.SubElement(rp,tag('b')); E.SubElement(rp,tag('bCs'))
    for i,line in enumerate(text.split('\n')):
        if i: E.SubElement(r,tag('br'))
        E.SubElement(r,tag('t')).text=line
    return p
# Preserve source cover art, metadata table, both section setups and all recurring furniture.
for c in list(body)[5:]: body.remove(c)
body.replace(body[2],paragraph('দয়াময়ী মন্দির\nব্যবহার নির্দেশিকা',54,True,children[2]))
cells=body[3].findall('.//w:tc',ns)
for cell,text in zip(cells,['কর্মী ও প্রশাসকদের জন্য\nসহজ বাংলা ব্যবহারবিধি','','সংস্করণ ১\n11 September 2026']):
    for p in cell.findall('w:p',ns): cell.remove(p)
    cell.append(paragraph(text,20))
body.append(paragraph('সূচিপত্র',40,True,heading))
body.append(paragraph('কাজের নাম দেখে সংশ্লিষ্ট পৃষ্ঠা খুলুন। নির্দেশগুলো ক্রমানুসারে অনুসরণ করুন।',24))
for i,page in enumerate(pages): body.append(paragraph(f'{page["title"]}    {i+3}',23))
for page in pages:
    body.append(paragraph(page['title'],42,True,heading,newpage=True,keep=True)); step=0
    for kind,text in page['parts']:
        if kind=='h':
            step=0; body.append(paragraph(text,29,True,keep=True))
        else:
            if kind=='s':
                step+=1; text=str(step).translate(str.maketrans('0123456789','০১২৩৪৫৬৭৮৯'))+'। '+text
            body.append(paragraph(text,24))
body.append(section)
parts['word/document.xml']=E.tostring(doc,xml_declaration=True,encoding='UTF-8',standalone=True)
for n in list(parts):
    if n.startswith('word/header') and n.endswith('.xml'):
        xml=E.fromstring(parts[n])
        for t in xml.findall('.//w:t',ns):
            if t.text=='Report title': t.text='দয়াময়ী মন্দির ব্যবহার নির্দেশিকা'
            elif t.text=='Date': t.text='11 September 2026'
        parts[n]=E.tostring(xml,xml_declaration=True,encoding='UTF-8',standalone=True)
with zipfile.ZipFile(output,'w',zipfile.ZIP_DEFLATED) as z:
    for n,data in parts.items(): z.writestr(n,data)
(root/'artifact.md').write_text(f'''# Template contract
Reference {reference}
SHA256 {hashlib.sha256(reference.read_bytes()).hexdigest()}
Reference has 6 pages, 2 letter-size sections, 1-inch margins. Source cover art, metadata table, section properties, relationships, images, numbering, styles and footer furniture retained. Recurring header text translated. Source heading and normal paragraph patterns cloned for manual chapters and explicit contents. Body font switched directly to Nirmala UI 12pt for Bengali readability; headings 21pt; cover 27pt. Original image is decorative template art, not represented as a temple photograph. Source filler report sections replaced with supported user instructions. All package parts other than document.xml and header XML preserved byte-for-byte. Contents use physical page numbers to be verified after Word export. LibreOffice renderer unavailable; installed Word export and PDF page rendering used for visual QA.
''',encoding='utf-8')
print(output)
