"""Extract the JPEG storyboard sheets from a yt-dlp .mhtml file. Usage: python -I mhtml_frames.py <file.mhtml> <outdir>"""
import base64, email, os, sys

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
msg = email.message_from_bytes(open(src, 'rb').read())
n = 0
for part in msg.walk():
    if part.get_content_type().startswith('image/'):
        data = part.get_payload(decode=True)
        if not data:
            continue
        n += 1
        open(os.path.join(out, 'sheet%03d.jpg' % n), 'wb').write(data)
print(n, 'sheets')
