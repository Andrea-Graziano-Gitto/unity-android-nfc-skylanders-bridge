import time
from pathlib import Path
import pyperclip
import pyautogui
import pygetwindow as gw
import win32gui
import win32con

current_slot_is_right = False

# Window titles support (Italian & English)
MANAGER_TITLES = ["Gestione Skylander", "Skylander Management", "Skylanders"]
DIALOG_TITLES = ["Seleziona file Skylander", "Select Skylander File", "Select Skylander file", "Select Skylander"]

def wait_for_any_window(titles, timeout=2.0):
    """Wait for a window matching any of the specified titles to appear."""
    start = time.time()
    while time.time() - start < timeout:
        for title in titles:
            windows = gw.getWindowsWithTitle(title)
            if windows:
                return windows[0]
        time.sleep(0.02)
    return None

def find_simulation_window():
    """Find the simulation window by looking for a Dolphin window that is not the main list or manager."""
    for win in gw.getAllWindows():
        title = win.title
        if (
            title
            and "Dolphin" in title
            and not any(mgr in title for mgr in MANAGER_TITLES)
            and not title.startswith("Dolphin 2606 a")
            and not title.startswith("Dolphin 5.")
        ):
            return win
    return None

def select_slot(target_slot_is_right):
    global current_slot_is_right

    if target_slot_is_right == current_slot_is_right:
        return

    pyautogui.press("right" if target_slot_is_right else "left")
    time.sleep(0.1)
    current_slot_is_right = target_slot_is_right

def prepare_manager(sim_win):
    sim_hwnd = sim_win._hWnd
    sim_win.restore()
    time.sleep(0.05)
    win32gui.ShowWindow(sim_hwnd, win32con.SW_MAXIMIZE)
    time.sleep(0.1)

    manager_window = wait_for_any_window(MANAGER_TITLES)
    if not manager_window:
        print("[ERRORE] Finestra Gestione Skylander non trovata (apri la finestra da Dolphin).")
        return None, None

    manager_window.restore()
    manager_window.activate()
    time.sleep(0.1)
    return sim_hwnd, manager_window

def return_to_simulation(sim_hwnd):
    time.sleep(0.4)
    try:
        win32gui.ShowWindow(sim_hwnd, win32con.SW_MAXIMIZE)
        win32gui.SetForegroundWindow(sim_hwnd)
    except Exception:
        pass

def clear_skylander_fast():
    try:
        sim_win = find_simulation_window()
        if not sim_win:
            print("[ERRORE] Finestra di simulazione Dolphin non trovata.")
            return

        sim_hwnd, manager_window = prepare_manager(sim_win)
        if not manager_window:
            return

        select_slot(True)
        pyautogui.press("space")
        return_to_simulation(sim_hwnd)
        print("[SUCCESSO] Slot NULL selezionato e focus ripristinato sulla simulazione!")

    except Exception as e:
        print(f"[ERRORE] {e}")

def load_skylander_fast(skylander_path):
    try:
        skylander_path = Path(skylander_path)

        if not skylander_path.is_file():
            print(f"[ERRORE] File Skylander non trovato: {skylander_path}")
            return

        # 1. Find the game's simulation window
        sim_win = find_simulation_window()
        if not sim_win:
            print("[ERRORE] Finestra di simulazione Dolphin non trovata.")
            return

        sim_hwnd, manager_window = prepare_manager(sim_win)
        if not manager_window:
            return

        select_slot(False)

        # Open the file selector for slot 0.
        pyautogui.press("space")

        # Wait for the dialog to appear
        dialog = wait_for_any_window(DIALOG_TITLES, timeout=2.0)
        if not dialog:
            print("[ERRORE] Finestra di selezione file Skylander non comparsa.")
            return

        dialog.activate()
        time.sleep(0.1)

        # Copy and paste the file path
        pyperclip.copy(str(skylander_path))
        pyautogui.hotkey("ctrl", "v")
        time.sleep(0.1)

        # Confirm
        pyautogui.press("enter")
        time.sleep(0.15)

        # Handle a possible "File in use" popup
        for dlg_title in DIALOG_TITLES:
            dialogs = gw.getWindowsWithTitle(dlg_title)
            if dialogs:
                pyautogui.press("enter")
                time.sleep(0.05)
                pyautogui.press("esc")
                break

        return_to_simulation(sim_hwnd)

        print(f"[SUCCESSO] Skylander ({skylander_path.name}) caricato e focus ripristinato!")

    except Exception as e:
        print(f"[ERRORE] {e}")

if __name__ == "__main__":
    # Example manual test:
    # load_skylander_fast(Path(r"C:\Users\YourUser\Documents\Dolphin Emulator\Skylanders\Spyro.sky"))
    pass