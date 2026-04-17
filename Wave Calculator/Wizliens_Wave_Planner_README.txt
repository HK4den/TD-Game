
Wizliens Wave Planner

What it does
- Create enemies with:
  - name
  - HP
  - speed
  - money on death
  - description / notes
- Create waves with:
  - wave name
  - completion reward
  - multiple spawn groups
  - enemy selection, count, spawn interval, delay after group
- Automatically calculates:
  - total HP
  - total enemy death money
  - total money gained including completion reward
  - spawn length
  - average enemy speed
  - per-group breakdown
- Save and load projects to continue later

How to run
1. Make sure Python 3 is installed.
2. Double click `wizliens_wave_planner.py`
   or run:
   python wizliens_wave_planner.py

Notes about wave length math
- This tool matches your provided Unity WaveSpawner closely:
  - it adds spawnInterval after EACH enemy spawn in a group
  - then adds delayAfterGroup after the group
- That means the "spawn length" here is the wave's spawn schedule length, not enemy travel-to-goal time.

Suggested next upgrades later
- Export a wave to a Unity-friendly JSON format
- Add search/filter for enemies
- Add tags like armored / flying / splitter / healer
- Add effective HP fields
- Add per-wave notes and difficulty rating
