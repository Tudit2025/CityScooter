"""Fetch explicitly selected CC0 sources; no login, API key or paid downloads.
Freesound files are public HQ MP3 previews, not the lossless original recordings.
Retain only source metadata; raw website HTML can contain third-party secrets.
"""
import concurrent.futures, hashlib, json, pathlib, re, urllib.request
from datetime import date

ROOT=pathlib.Path(__file__).resolve().parents[1]/'audio-src/comic-city'
SOURCES=[
 dict(id='city',title='City ambience',author='DCphotofilm',page='https://freesound.org/people/DCphotofilm/sounds/411604/'),
 dict(id='rolling',title='SkateRolling.wav',author='Lanterr',page='https://freesound.org/people/Lanterr/sounds/688733/'),
 dict(id='horn_a',title='Car Horn',author='danlucaz',page='https://freesound.org/people/danlucaz/sounds/517673/'),
 dict(id='horn_b',title='Car Horn Honk.wav',author='DeVern',page='https://freesound.org/people/DeVern/sounds/349922/'),
 dict(id='scrape',title='Metal Scrape',author='magnuswaker',page='https://freesound.org/people/magnuswaker/sounds/530075/'),
 dict(id='music',title='Hot Roadway (160 BPM)',author='MintoDog',page='https://opengameart.org/content/hot-roadway',download='https://opengameart.org/sites/default/files/hot_roadway_bpm160.flac'),
]
def request(url):
    return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0 (CityScooter audio asset preparation)'}),timeout=90).read()
def write_source_record(item):
    fields=('id','title','author','page','download','file','license','license_url','retrieved','sha256','delivery')
    folder=ROOT/'originals';folder.mkdir(parents=True,exist_ok=True)
    record='CityScooter audio source metadata (not a page archive)\n\n'
    record+=''.join(f'{key}: {item[key]}\n' for key in fields)
    (folder/(item['id']+'-source.txt')).write_text(record,encoding='utf-8')

def fetch(item):
    item=item.copy();folder=ROOT/'originals';folder.mkdir(parents=True,exist_ok=True)
    page=request(item['page']).decode('utf-8')
    if not ('CC0' in page or 'Creative Commons 0' in page or 'publicdomain/zero' in page):
        raise RuntimeError('License check required: '+item['page'])
    if 'download' not in item:
        matches=re.findall(r'https://cdn\.freesound\.org/previews/[^\s"<>]+-hq\.mp3',page)
        if not matches:raise RuntimeError('No public preview: '+item['id'])
        item['download']=matches[0]
    ext=pathlib.Path(item['download']).suffix;path=folder/(item['id']+ext)
    if not path.exists():path.write_bytes(request(item['download']))
    item.update(file='originals/'+path.name,license='CC0 1.0',license_url='https://creativecommons.org/publicdomain/zero/1.0/',retrieved=date.today().isoformat(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),delivery='lossless FLAC' if ext=='.flac' else 'public HQ MP3 preview (lossy)')
    write_source_record(item)
    print(item['id'],path.stat().st_size,flush=True);return item
if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:items=list(pool.map(fetch,SOURCES))
    (ROOT/'sources.json').write_text(json.dumps(items,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
