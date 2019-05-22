#!/usr/bin/env python

import os
aelf_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))

files = [os.path.join(rt, f)
    for rt, _, fls in os.walk(aelf_dir)
    for f in fls
    if f.endswith('.c.cs') or f.endswith('.g.cs')
]

for f in files:
    os.remove(f)
