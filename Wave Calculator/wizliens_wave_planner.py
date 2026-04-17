
import json
import tkinter as tk
from tkinter import ttk, messagebox, filedialog


APP_TITLE = "Wizliens Wave Planner"


class EnemyEditor(tk.Toplevel):
    def __init__(self, master, enemy=None, on_save=None):
        super().__init__(master)
        self.title("Enemy Editor")
        self.resizable(False, False)
        self.on_save = on_save
        self.enemy = enemy or {
            "name": "",
            "hp": 10.0,
            "speed": 2.5,
            "money_on_death": 1.0,
            "description": ""
        }

        self.name_var = tk.StringVar(value=self.enemy.get("name", ""))
        self.hp_var = tk.StringVar(value=str(self.enemy.get("hp", 10.0)))
        self.speed_var = tk.StringVar(value=str(self.enemy.get("speed", 2.5)))
        self.money_var = tk.StringVar(value=str(self.enemy.get("money_on_death", 1.0)))

        root = ttk.Frame(self, padding=12)
        root.grid(sticky="nsew")

        ttk.Label(root, text="Name").grid(row=0, column=0, sticky="w")
        ttk.Entry(root, textvariable=self.name_var, width=36).grid(row=1, column=0, sticky="ew", pady=(0, 8))

        row2 = ttk.Frame(root)
        row2.grid(row=2, column=0, sticky="ew", pady=(0, 8))
        row2.columnconfigure((0, 1, 2), weight=1)

        ttk.Label(row2, text="HP").grid(row=0, column=0, sticky="w")
        ttk.Label(row2, text="Speed").grid(row=0, column=1, sticky="w")
        ttk.Label(row2, text="Money on Death").grid(row=0, column=2, sticky="w")

        ttk.Entry(row2, textvariable=self.hp_var, width=12).grid(row=1, column=0, padx=(0, 8), sticky="ew")
        ttk.Entry(row2, textvariable=self.speed_var, width=12).grid(row=1, column=1, padx=(0, 8), sticky="ew")
        ttk.Entry(row2, textvariable=self.money_var, width=14).grid(row=1, column=2, sticky="ew")

        ttk.Label(root, text="Description / Notes").grid(row=3, column=0, sticky="w")
        self.desc_text = tk.Text(root, width=50, height=10, wrap="word")
        self.desc_text.grid(row=4, column=0, sticky="ew")
        self.desc_text.insert("1.0", self.enemy.get("description", ""))

        btns = ttk.Frame(root)
        btns.grid(row=5, column=0, sticky="e", pady=(10, 0))
        ttk.Button(btns, text="Cancel", command=self.destroy).pack(side="right")
        ttk.Button(btns, text="Save", command=self.save_enemy).pack(side="right", padx=(0, 8))

        self.transient(master)
        self.grab_set()
        self.focus()

    def save_enemy(self):
        try:
            enemy = {
                "name": self.name_var.get().strip(),
                "hp": float(self.hp_var.get()),
                "speed": float(self.speed_var.get()),
                "money_on_death": float(self.money_var.get()),
                "description": self.desc_text.get("1.0", "end").strip()
            }
        except ValueError:
            messagebox.showerror("Invalid Number", "HP, Speed, and Money on Death must be valid numbers.")
            return

        if not enemy["name"]:
            messagebox.showerror("Missing Name", "Enemy needs a name.")
            return

        if self.on_save:
            self.on_save(enemy)
        self.destroy()


class GroupEditor(tk.Toplevel):
    def __init__(self, master, enemy_names, group=None, on_save=None):
        super().__init__(master)
        self.title("Spawn Group Editor")
        self.resizable(False, False)
        self.on_save = on_save
        self.enemy_names = enemy_names
        self.group = group or {
            "enemy_name": enemy_names[0] if enemy_names else "",
            "count": 10,
            "spawn_interval": 0.6,
            "delay_after_group": 0.0
        }

        self.enemy_var = tk.StringVar(value=self.group.get("enemy_name", ""))
        self.count_var = tk.StringVar(value=str(self.group.get("count", 10)))
        self.interval_var = tk.StringVar(value=str(self.group.get("spawn_interval", 0.6)))
        self.delay_var = tk.StringVar(value=str(self.group.get("delay_after_group", 0.0)))

        root = ttk.Frame(self, padding=12)
        root.grid(sticky="nsew")

        ttk.Label(root, text="Enemy").grid(row=0, column=0, sticky="w")
        enemy_combo = ttk.Combobox(root, textvariable=self.enemy_var, values=self.enemy_names, width=32, state="readonly")
        enemy_combo.grid(row=1, column=0, sticky="ew", pady=(0, 8))

        row2 = ttk.Frame(root)
        row2.grid(row=2, column=0, sticky="ew", pady=(0, 8))
        row2.columnconfigure((0, 1, 2), weight=1)

        ttk.Label(row2, text="Count").grid(row=0, column=0, sticky="w")
        ttk.Label(row2, text="Spawn Interval").grid(row=0, column=1, sticky="w")
        ttk.Label(row2, text="Delay After Group").grid(row=0, column=2, sticky="w")

        ttk.Entry(row2, textvariable=self.count_var, width=12).grid(row=1, column=0, padx=(0, 8), sticky="ew")
        ttk.Entry(row2, textvariable=self.interval_var, width=12).grid(row=1, column=1, padx=(0, 8), sticky="ew")
        ttk.Entry(row2, textvariable=self.delay_var, width=14).grid(row=1, column=2, sticky="ew")

        btns = ttk.Frame(root)
        btns.grid(row=3, column=0, sticky="e", pady=(10, 0))
        ttk.Button(btns, text="Cancel", command=self.destroy).pack(side="right")
        ttk.Button(btns, text="Save", command=self.save_group).pack(side="right", padx=(0, 8))

        self.transient(master)
        self.grab_set()
        self.focus()

    def save_group(self):
        try:
            group = {
                "enemy_name": self.enemy_var.get().strip(),
                "count": int(self.count_var.get()),
                "spawn_interval": float(self.interval_var.get()),
                "delay_after_group": float(self.delay_var.get())
            }
        except ValueError:
            messagebox.showerror("Invalid Number", "Count must be an integer. Timing values must be numbers.")
            return

        if not group["enemy_name"]:
            messagebox.showerror("Missing Enemy", "Choose an enemy for this group.")
            return

        if group["count"] <= 0:
            messagebox.showerror("Invalid Count", "Count must be above 0.")
            return

        if self.on_save:
            self.on_save(group)
        self.destroy()


class WavePlannerApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title(APP_TITLE)
        self.geometry("1400x760")
        self.minsize(1180, 700)

        self.data = {
            "meta": {"project_name": "Wizliens Wave Planner"},
            "settings": {"default_completion_reward": 50},
            "enemies": [],
            "waves": []
        }

        self.current_file = None

        self._build_ui()
        self.refresh_all()

    def _build_ui(self):
        self.columnconfigure(0, weight=1)
        self.rowconfigure(1, weight=1)

        topbar = ttk.Frame(self, padding=8)
        topbar.grid(row=0, column=0, sticky="ew")
        for i in range(20):
            topbar.columnconfigure(i, weight=0)
        topbar.columnconfigure(19, weight=1)

        ttk.Button(topbar, text="New", command=self.new_project).grid(row=0, column=0, padx=(0, 6))
        ttk.Button(topbar, text="Open", command=self.open_project).grid(row=0, column=1, padx=(0, 6))
        ttk.Button(topbar, text="Save", command=self.save_project).grid(row=0, column=2, padx=(0, 6))
        ttk.Button(topbar, text="Save As", command=self.save_project_as).grid(row=0, column=3, padx=(0, 12))

        ttk.Label(topbar, text="Default End-of-Wave Reward").grid(row=0, column=4, padx=(0, 6))
        self.default_reward_var = tk.StringVar(value="50")
        reward_entry = ttk.Entry(topbar, textvariable=self.default_reward_var, width=10)
        reward_entry.grid(row=0, column=5, padx=(0, 8))
        ttk.Button(topbar, text="Apply", command=self.apply_default_reward).grid(row=0, column=6, padx=(0, 8))

        self.file_label = ttk.Label(topbar, text="Unsaved project")
        self.file_label.grid(row=0, column=19, sticky="e")

        main = ttk.Panedwindow(self, orient="horizontal")
        main.grid(row=1, column=0, sticky="nsew", padx=8, pady=(0, 8))

        left = ttk.Frame(main, padding=8)
        center = ttk.Frame(main, padding=8)
        right = ttk.Frame(main, padding=8)

        main.add(left, weight=1)
        main.add(center, weight=1)
        main.add(right, weight=1)

        self._build_enemy_panel(left)
        self._build_wave_panel(center)
        self._build_summary_panel(right)

    def _build_enemy_panel(self, parent):
        parent.rowconfigure(1, weight=1)
        parent.columnconfigure(0, weight=1)

        ttk.Label(parent, text="Enemies", font=("Segoe UI", 13, "bold")).grid(row=0, column=0, sticky="w", pady=(0, 8))

        self.enemy_list = tk.Listbox(parent, exportselection=False)
        self.enemy_list.grid(row=1, column=0, sticky="nsew")
        self.enemy_list.bind("<<ListboxSelect>>", lambda e: self.show_selected_enemy())

        enemy_btns = ttk.Frame(parent)
        enemy_btns.grid(row=2, column=0, sticky="ew", pady=(8, 8))
        ttk.Button(enemy_btns, text="Add Enemy", command=self.add_enemy).pack(side="left")
        ttk.Button(enemy_btns, text="Edit Enemy", command=self.edit_enemy).pack(side="left", padx=(6, 0))
        ttk.Button(enemy_btns, text="Delete Enemy", command=self.delete_enemy).pack(side="left", padx=(6, 0))

        ttk.Label(parent, text="Enemy Details", font=("Segoe UI", 11, "bold")).grid(row=3, column=0, sticky="w")
        self.enemy_details = tk.Text(parent, width=40, height=18, wrap="word", state="disabled")
        self.enemy_details.grid(row=4, column=0, sticky="nsew", pady=(6, 0))
        parent.rowconfigure(4, weight=1)

    def _build_wave_panel(self, parent):
        parent.rowconfigure(1, weight=1)
        parent.rowconfigure(5, weight=1)
        parent.columnconfigure(0, weight=1)

        ttk.Label(parent, text="Waves", font=("Segoe UI", 13, "bold")).grid(row=0, column=0, sticky="w", pady=(0, 8))

        self.wave_list = tk.Listbox(parent, exportselection=False)
        self.wave_list.grid(row=1, column=0, sticky="nsew")
        self.wave_list.bind("<<ListboxSelect>>", lambda e: self.on_wave_selected())

        wave_btns = ttk.Frame(parent)
        wave_btns.grid(row=2, column=0, sticky="ew", pady=(8, 8))
        ttk.Button(wave_btns, text="Add Wave", command=self.add_wave).pack(side="left")
        ttk.Button(wave_btns, text="Edit Wave Name/Reward", command=self.edit_wave).pack(side="left", padx=(6, 0))
        ttk.Button(wave_btns, text="Delete Wave", command=self.delete_wave).pack(side="left", padx=(6, 0))

        header = ttk.Frame(parent)
        header.grid(row=3, column=0, sticky="ew", pady=(4, 6))
        header.columnconfigure(0, weight=1)

        ttk.Label(header, text="Selected Wave Groups", font=("Segoe UI", 11, "bold")).grid(row=0, column=0, sticky="w")
        self.selected_wave_meta = ttk.Label(header, text="")
        self.selected_wave_meta.grid(row=0, column=1, sticky="e")

        self.group_list = tk.Listbox(parent, exportselection=False)
        self.group_list.grid(row=5, column=0, sticky="nsew")

        group_btns = ttk.Frame(parent)
        group_btns.grid(row=6, column=0, sticky="ew", pady=(8, 0))
        ttk.Button(group_btns, text="Add Group", command=self.add_group).pack(side="left")
        ttk.Button(group_btns, text="Edit Group", command=self.edit_group).pack(side="left", padx=(6, 0))
        ttk.Button(group_btns, text="Delete Group", command=self.delete_group).pack(side="left", padx=(6, 0))
        ttk.Button(group_btns, text="Move Up", command=lambda: self.move_group(-1)).pack(side="left", padx=(12, 0))
        ttk.Button(group_btns, text="Move Down", command=lambda: self.move_group(1)).pack(side="left", padx=(6, 0))

    def _build_summary_panel(self, parent):
        parent.rowconfigure(3, weight=1)
        parent.columnconfigure(0, weight=1)

        ttk.Label(parent, text="Math / Summary", font=("Segoe UI", 13, "bold")).grid(row=0, column=0, sticky="w", pady=(0, 8))

        self.summary_frame = ttk.LabelFrame(parent, text="Selected Wave Totals", padding=10)
        self.summary_frame.grid(row=1, column=0, sticky="ew")

        self.summary_vars = {
            "groups": tk.StringVar(value="0"),
            "enemy_count": tk.StringVar(value="0"),
            "total_hp": tk.StringVar(value="0"),
            "enemy_money": tk.StringVar(value="0"),
            "completion_reward": tk.StringVar(value="0"),
            "grand_total_money": tk.StringVar(value="0"),
            "spawn_length": tk.StringVar(value="0.00 s"),
            "avg_speed": tk.StringVar(value="0"),
        }

        row = 0
        for label, key in [
            ("Groups", "groups"),
            ("Enemies in Wave", "enemy_count"),
            ("Total HP", "total_hp"),
            ("Enemy Death Money", "enemy_money"),
            ("Completion Reward", "completion_reward"),
            ("Total Money Gained", "grand_total_money"),
            ("Spawn Length", "spawn_length"),
            ("Average Enemy Speed", "avg_speed"),
        ]:
            ttk.Label(self.summary_frame, text=label).grid(row=row, column=0, sticky="w", pady=2)
            ttk.Label(self.summary_frame, textvariable=self.summary_vars[key]).grid(row=row, column=1, sticky="e", pady=2)
            row += 1

        ttk.Label(parent, text="Wave Breakdown", font=("Segoe UI", 11, "bold")).grid(row=2, column=0, sticky="w", pady=(10, 6))
        self.breakdown_text = tk.Text(parent, width=50, height=18, wrap="word", state="disabled")
        self.breakdown_text.grid(row=3, column=0, sticky="nsew")

        tips = ttk.LabelFrame(parent, text="Good extra fields to maybe add later", padding=10)
        tips.grid(row=4, column=0, sticky="ew", pady=(10, 0))
        ttk.Label(
            tips,
            text="Suggested later: armor, tags, gimmick flags, lane count assumptions, leak danger notes, and expected path time."
        ).grid(row=0, column=0, sticky="w")

    def format_number(self, value):
        if abs(value - round(value)) < 1e-9:
            return str(int(round(value)))
        return f"{value:.2f}"

    def get_selected_enemy_index(self):
        sel = self.enemy_list.curselection()
        return sel[0] if sel else None

    def get_selected_wave_index(self):
        sel = self.wave_list.curselection()
        return sel[0] if sel else None

    def get_selected_group_index(self):
        sel = self.group_list.curselection()
        return sel[0] if sel else None

    def add_enemy(self):
        def on_save(enemy):
            self.data["enemies"].append(enemy)
            self.refresh_all(select_enemy=len(self.data["enemies"]) - 1)
        EnemyEditor(self, on_save=on_save)

    def edit_enemy(self):
        idx = self.get_selected_enemy_index()
        if idx is None:
            messagebox.showinfo("No Enemy Selected", "Select an enemy first.")
            return

        original_name = self.data["enemies"][idx]["name"]

        def on_save(enemy):
            self.data["enemies"][idx] = enemy
            new_name = enemy["name"]
            if original_name != new_name:
                for wave in self.data["waves"]:
                    for group in wave.get("groups", []):
                        if group.get("enemy_name") == original_name:
                            group["enemy_name"] = new_name
            self.refresh_all(select_enemy=idx)

        EnemyEditor(self, enemy=self.data["enemies"][idx], on_save=on_save)

    def delete_enemy(self):
        idx = self.get_selected_enemy_index()
        if idx is None:
            messagebox.showinfo("No Enemy Selected", "Select an enemy first.")
            return

        enemy_name = self.data["enemies"][idx]["name"]
        used_in = []
        for wave in self.data["waves"]:
            for group in wave.get("groups", []):
                if group.get("enemy_name") == enemy_name:
                    used_in.append(wave["name"])
                    break

        if used_in:
            messagebox.showerror(
                "Enemy In Use",
                f"Can't delete '{enemy_name}' because it is used in: {', '.join(used_in)}"
            )
            return

        del self.data["enemies"][idx]
        self.refresh_all()

    def show_selected_enemy(self):
        idx = self.get_selected_enemy_index()
        if idx is None:
            self._set_text(self.enemy_details, "")
            return

        enemy = self.data["enemies"][idx]
        lines = [
            f"Name: {enemy['name']}",
            f"HP: {self.format_number(enemy['hp'])}",
            f"Speed: {self.format_number(enemy['speed'])}",
            f"Money on Death: {self.format_number(enemy['money_on_death'])}",
            "",
            "Description / Notes:",
            enemy.get("description", "")
        ]
        self._set_text(self.enemy_details, "\n".join(lines))

    def add_wave(self):
        default_reward = self.data["settings"].get("default_completion_reward", 50)
        dialog = WaveMetaDialog(self, title="Add Wave", wave={"name": f"Wave {len(self.data['waves']) + 1}", "completion_reward": default_reward})
        self.wait_window(dialog)
        if dialog.result:
            self.data["waves"].append({
                "name": dialog.result["name"],
                "completion_reward": dialog.result["completion_reward"],
                "groups": []
            })
            self.refresh_all(select_wave=len(self.data["waves"]) - 1)

    def edit_wave(self):
        idx = self.get_selected_wave_index()
        if idx is None:
            messagebox.showinfo("No Wave Selected", "Select a wave first.")
            return

        wave = self.data["waves"][idx]
        dialog = WaveMetaDialog(self, title="Edit Wave", wave=wave)
        self.wait_window(dialog)
        if dialog.result:
            wave["name"] = dialog.result["name"]
            wave["completion_reward"] = dialog.result["completion_reward"]
            self.refresh_all(select_wave=idx)

    def delete_wave(self):
        idx = self.get_selected_wave_index()
        if idx is None:
            messagebox.showinfo("No Wave Selected", "Select a wave first.")
            return

        del self.data["waves"][idx]
        self.refresh_all()

    def on_wave_selected(self):
        self.refresh_groups()
        self.refresh_summary()

    def add_group(self):
        wave_idx = self.get_selected_wave_index()
        if wave_idx is None:
            messagebox.showinfo("No Wave Selected", "Select a wave first.")
            return

        enemy_names = [e["name"] for e in self.data["enemies"]]
        if not enemy_names:
            messagebox.showinfo("No Enemies Yet", "Create at least one enemy before adding a group.")
            return

        def on_save(group):
            self.data["waves"][wave_idx]["groups"].append(group)
            self.refresh_all(select_wave=wave_idx, select_group=len(self.data["waves"][wave_idx]["groups"]) - 1)

        GroupEditor(self, enemy_names=enemy_names, on_save=on_save)

    def edit_group(self):
        wave_idx = self.get_selected_wave_index()
        group_idx = self.get_selected_group_index()
        if wave_idx is None or group_idx is None:
            messagebox.showinfo("No Group Selected", "Select a group first.")
            return

        enemy_names = [e["name"] for e in self.data["enemies"]]
        if not enemy_names:
            messagebox.showinfo("No Enemies", "There are no enemies to choose from.")
            return

        group = self.data["waves"][wave_idx]["groups"][group_idx]

        def on_save(new_group):
            self.data["waves"][wave_idx]["groups"][group_idx] = new_group
            self.refresh_all(select_wave=wave_idx, select_group=group_idx)

        GroupEditor(self, enemy_names=enemy_names, group=group, on_save=on_save)

    def delete_group(self):
        wave_idx = self.get_selected_wave_index()
        group_idx = self.get_selected_group_index()
        if wave_idx is None or group_idx is None:
            messagebox.showinfo("No Group Selected", "Select a group first.")
            return

        del self.data["waves"][wave_idx]["groups"][group_idx]
        self.refresh_all(select_wave=wave_idx)

    def move_group(self, direction):
        wave_idx = self.get_selected_wave_index()
        group_idx = self.get_selected_group_index()
        if wave_idx is None or group_idx is None:
            return

        groups = self.data["waves"][wave_idx]["groups"]
        new_idx = group_idx + direction
        if new_idx < 0 or new_idx >= len(groups):
            return

        groups[group_idx], groups[new_idx] = groups[new_idx], groups[group_idx]
        self.refresh_all(select_wave=wave_idx, select_group=new_idx)

    def _enemy_lookup(self):
        return {enemy["name"]: enemy for enemy in self.data["enemies"]}

    def calculate_wave(self, wave):
        enemies = self._enemy_lookup()
        total_hp = 0.0
        total_enemy_money = 0.0
        total_count = 0
        total_spawn_length = 0.0
        weighted_speed_sum = 0.0
        breakdown = []

        for i, group in enumerate(wave.get("groups", []), start=1):
            enemy = enemies.get(group.get("enemy_name"))
            if not enemy:
                breakdown.append(f"Group {i}: Missing enemy '{group.get('enemy_name', '')}'")
                continue

            count = int(group.get("count", 0))
            spawn_interval = float(group.get("spawn_interval", 0.0))
            delay_after_group = float(group.get("delay_after_group", 0.0))

            group_hp = enemy["hp"] * count
            group_money = enemy["money_on_death"] * count

            # Matches the provided Unity spawner closely:
            # it waits spawnInterval after EACH spawn, including the last one in the group,
            # then waits delayAfterGroup after the group ends.
            group_spawn_time = (max(0.0, spawn_interval) * count) + max(0.0, delay_after_group)

            total_hp += group_hp
            total_enemy_money += group_money
            total_count += count
            total_spawn_length += group_spawn_time
            weighted_speed_sum += enemy["speed"] * count

            breakdown.append(
                f"Group {i}: {enemy['name']} x{count} | HP {self.format_number(group_hp)} | "
                f"Death Money {self.format_number(group_money)} | "
                f"Time {self.format_number(group_spawn_time)}s "
                f"(interval {self.format_number(spawn_interval)}, delay {self.format_number(delay_after_group)})"
            )

        completion_reward = float(wave.get("completion_reward", 0.0))
        grand_total_money = total_enemy_money + completion_reward
        avg_speed = weighted_speed_sum / total_count if total_count > 0 else 0.0

        return {
            "groups": len(wave.get("groups", [])),
            "enemy_count": total_count,
            "total_hp": total_hp,
            "enemy_money": total_enemy_money,
            "completion_reward": completion_reward,
            "grand_total_money": grand_total_money,
            "spawn_length": total_spawn_length,
            "avg_speed": avg_speed,
            "breakdown": breakdown
        }

    def refresh_enemies(self, select_enemy=None):
        self.enemy_list.delete(0, "end")
        for enemy in self.data["enemies"]:
            self.enemy_list.insert("end", enemy["name"])

        if select_enemy is not None and 0 <= select_enemy < self.enemy_list.size():
            self.enemy_list.selection_set(select_enemy)
            self.enemy_list.activate(select_enemy)

        self.show_selected_enemy()

    def refresh_waves(self, select_wave=None):
        self.wave_list.delete(0, "end")
        for i, wave in enumerate(self.data["waves"], start=1):
            self.wave_list.insert("end", f"{i}. {wave['name']}")

        if select_wave is not None and 0 <= select_wave < self.wave_list.size():
            self.wave_list.selection_set(select_wave)
            self.wave_list.activate(select_wave)

    def refresh_groups(self, select_group=None):
        self.group_list.delete(0, "end")
        wave_idx = self.get_selected_wave_index()
        if wave_idx is None:
            self.selected_wave_meta.config(text="")
            return

        wave = self.data["waves"][wave_idx]
        self.selected_wave_meta.config(text=f"Reward: {self.format_number(wave.get('completion_reward', 0))}")

        for i, group in enumerate(wave.get("groups", []), start=1):
            self.group_list.insert(
                "end",
                f"{i}. {group['enemy_name']} x{group['count']} | interval {self.format_number(group['spawn_interval'])} | delay {self.format_number(group['delay_after_group'])}"
            )

        if select_group is not None and 0 <= select_group < self.group_list.size():
            self.group_list.selection_set(select_group)
            self.group_list.activate(select_group)

    def refresh_summary(self):
        wave_idx = self.get_selected_wave_index()
        if wave_idx is None:
            for var in self.summary_vars.values():
                var.set("0")
            self.summary_vars["spawn_length"].set("0.00 s")
            self._set_text(self.breakdown_text, "")
            return

        wave = self.data["waves"][wave_idx]
        result = self.calculate_wave(wave)

        self.summary_vars["groups"].set(str(result["groups"]))
        self.summary_vars["enemy_count"].set(str(result["enemy_count"]))
        self.summary_vars["total_hp"].set(self.format_number(result["total_hp"]))
        self.summary_vars["enemy_money"].set(self.format_number(result["enemy_money"]))
        self.summary_vars["completion_reward"].set(self.format_number(result["completion_reward"]))
        self.summary_vars["grand_total_money"].set(self.format_number(result["grand_total_money"]))
        self.summary_vars["spawn_length"].set(f"{self.format_number(result['spawn_length'])} s")
        self.summary_vars["avg_speed"].set(self.format_number(result["avg_speed"]))

        self._set_text(self.breakdown_text, "\n".join(result["breakdown"]))

    def refresh_all(self, select_enemy=None, select_wave=None, select_group=None):
        self.refresh_enemies(select_enemy=select_enemy)

        previous_wave_idx = self.get_selected_wave_index()
        self.refresh_waves(select_wave=select_wave if select_wave is not None else previous_wave_idx)

        if select_wave is not None and 0 <= select_wave < self.wave_list.size():
            self.wave_list.selection_clear(0, "end")
            self.wave_list.selection_set(select_wave)
            self.wave_list.activate(select_wave)

        self.refresh_groups(select_group=select_group)
        self.refresh_summary()
        self.update_file_label()

    def _set_text(self, widget, text):
        widget.config(state="normal")
        widget.delete("1.0", "end")
        widget.insert("1.0", text)
        widget.config(state="disabled")

    def apply_default_reward(self):
        try:
            value = float(self.default_reward_var.get())
        except ValueError:
            messagebox.showerror("Invalid Reward", "Default reward must be a valid number.")
            return

        self.data["settings"]["default_completion_reward"] = value
        messagebox.showinfo("Applied", "Default reward updated for future new waves.")

    def new_project(self):
        if not self.confirm_discard():
            return
        self.data = {
            "meta": {"project_name": "Wizliens Wave Planner"},
            "settings": {"default_completion_reward": 50},
            "enemies": [],
            "waves": []
        }
        self.current_file = None
        self.default_reward_var.set("50")
        self.refresh_all()

    def save_project(self):
        if self.current_file is None:
            self.save_project_as()
            return
        self._write_project(self.current_file)

    def save_project_as(self):
        path = filedialog.asksaveasfilename(
            title="Save Project",
            defaultextension=".wizwaves",
            filetypes=[("Wizliens Wave Planner", "*.wizwaves"), ("JSON files", "*.json"), ("All files", "*.*")]
        )
        if not path:
            return
        self.current_file = path
        self._write_project(path)

    def _write_project(self, path):
        try:
            with open(path, "w", encoding="utf-8") as f:
                json.dump(self.data, f, indent=2)
            self.update_file_label()
            messagebox.showinfo("Saved", f"Project saved:\n{path}")
        except Exception as e:
            messagebox.showerror("Save Failed", str(e))

    def open_project(self):
        if not self.confirm_discard():
            return
        path = filedialog.askopenfilename(
            title="Open Project",
            filetypes=[("Wizliens Wave Planner", "*.wizwaves"), ("JSON files", "*.json"), ("All files", "*.*")]
        )
        if not path:
            return

        try:
            with open(path, "r", encoding="utf-8") as f:
                self.data = json.load(f)
            self.current_file = path
            self.default_reward_var.set(str(self.data.get("settings", {}).get("default_completion_reward", 50)))
            self.refresh_all()
        except Exception as e:
            messagebox.showerror("Open Failed", str(e))

    def update_file_label(self):
        self.file_label.config(text=self.current_file if self.current_file else "Unsaved project")

    def confirm_discard(self):
        return messagebox.askyesno("Continue?", "Unsaved changes in memory will be lost. Continue?")

class WaveMetaDialog(tk.Toplevel):
    def __init__(self, master, title, wave):
        super().__init__(master)
        self.title(title)
        self.resizable(False, False)
        self.result = None

        self.name_var = tk.StringVar(value=wave.get("name", "Wave"))
        self.reward_var = tk.StringVar(value=str(wave.get("completion_reward", 50)))

        root = ttk.Frame(self, padding=12)
        root.grid(sticky="nsew")

        ttk.Label(root, text="Wave Name").grid(row=0, column=0, sticky="w")
        ttk.Entry(root, textvariable=self.name_var, width=34).grid(row=1, column=0, sticky="ew", pady=(0, 8))

        ttk.Label(root, text="Completion Reward").grid(row=2, column=0, sticky="w")
        ttk.Entry(root, textvariable=self.reward_var, width=20).grid(row=3, column=0, sticky="w")

        btns = ttk.Frame(root)
        btns.grid(row=4, column=0, sticky="e", pady=(10, 0))
        ttk.Button(btns, text="Cancel", command=self.destroy).pack(side="right")
        ttk.Button(btns, text="Save", command=self.save).pack(side="right", padx=(0, 8))

        self.transient(master)
        self.grab_set()
        self.focus()

    def save(self):
        try:
            reward = float(self.reward_var.get())
        except ValueError:
            messagebox.showerror("Invalid Reward", "Completion reward must be a valid number.")
            return

        name = self.name_var.get().strip()
        if not name:
            messagebox.showerror("Missing Name", "Wave needs a name.")
            return

        self.result = {"name": name, "completion_reward": reward}
        self.destroy()


if __name__ == "__main__":
    app = WavePlannerApp()
    app.mainloop()
