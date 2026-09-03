import csv
import io
import json
from pathlib import Path
import time
import requests

from Sostitutore import clear_skylander_fast, load_skylander_fast

# Your Google Sheet configuration data
SHEET_ID = "YOUR_GOOGLE_SHEET_ID"
GID = "YOUR_GOOGLE_SHEET_GID"
URL = f"https://docs.google.com/spreadsheets/d/{SHEET_ID}/gviz/tq?tqx=out:csv&gid={GID}"
BASE_DIR = Path(__file__).resolve().parent
MAPPING_PATH = BASE_DIR / "mappatura_skylander.json"

with MAPPING_PATH.open(encoding="utf-8") as mapping_file:
  skylander_mapping = json.load(mapping_file)

last_processed_value = None

print("In ascolto del Google Sheet in tempo reale...")

while True:
  try:
    response = requests.get(URL, timeout=5)
    if response.status_code == 200:
      # Read the CSV content downloaded from the sheet
      csv_data = response.content.decode("utf-8")
      f = io.StringIO(csv_data)
      reader = csv.reader(f)
      rows = list(reader)

      # Check whether there is data beyond the header (row 0)
      if len(rows) > 1:
        data_rows = rows[1:]  # Skip the header

        # Filter valid rows (with a date in column 0 and a value in column 1)
        valid_rows = [r for r in data_rows if len(r) >= 2 and r[0].strip()]

        if valid_rows:
          # Google Forms always puts the date/time in the first column (column 0).
          # Sort rows by date/time to get the latest one in chronological order.
          latest_row = max(valid_rows, key=lambda x: x[0])

          timestamp = latest_row[0]
          current_value = latest_row[1]  # The NFC serial number or submitted text

          # Perform the action only if the value differs from the last processed one
          if current_value != last_processed_value:
            print(f"[{timestamp}] Nuovo Tag Ricevuto: {current_value}")
            last_processed_value = current_value

            skylander_file = skylander_mapping.get(current_value.strip())
            if skylander_file == "NULL":
              clear_skylander_fast()
            elif skylander_file:
              load_skylander_fast(BASE_DIR / skylander_file)
            else:
              print(f"[INFO] Nessuna mappatura trovata per il seriale: {current_value}")

    else:
      print(f"Errore HTTP: {response.status_code}")

  except Exception as e:
    print(f"Errore di connessione/lettura: {e}")

  # Polling interval (0.5 seconds)
  time.sleep(0.5)