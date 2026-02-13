# Playmode Manual Apply Tool (Unity Editor Extension)

A lightweight Unity Editor tool that allows you to **manually edit objects during Play Mode**, track those changes, and then **apply them back into Edit Mode** with a single click.

Built for simulation-heavy workflows where live runtime tweaking is essential — without losing changes when exiting Play Mode.

---

## 🎯 Motivation

I built this tool because I got genuinely annoyed seeing a developer selling the exact same functionality for **$8 USD**.

Unity already provides most of the backend needed for this (Undo-based tracking), so I decided to create a clean and lightweight version and share it publicly so developers can stop paying for something that should be free.

If you find it useful, feel free to support the project via donation.

---

## ✨ Key Features

### ✅ Tracks Manual Play Mode Changes Only
This tool records only the changes you manually make during Play Mode, such as:
- Inspector value edits
- Scene gizmo movement (Move/Rotate/Scale)
- Adding components using **Add Component**
- Collider size adjustments

It does **NOT** detect automatic script-driven movement or runtime logic changes.

---

## 🔥 Supported Components & Data

### 🟦 Transform Data
- `Transform` position / rotation / scale  
- `RectTransform` position / rotation / scale (including serialized UI properties)

### 🟩 Colliders (All Types)
Automatically works with:
- BoxCollider
- SphereCollider
- CapsuleCollider
- MeshCollider
- WheelCollider
- Any custom collider component

All collider fields are captured through serialized data.

### 🧩 Rendering & Mesh Data
- MeshRenderer settings (materials, flags, etc.)
- MeshFilter mesh references

### 🧠 Scripts / MonoBehaviours
- Any script fields modified manually via Inspector
- Any components added during Play Mode via Add Component

---

## 🎥 Demo Video

Click below to watch the demo:

[▶ Watch Demo Video](https://www.linkedin.com/posts/shounak00_unity-gamedev-xr-activity-7428225860488531969-U1K9)

---

## 🧠 Workflow

### During Play Mode
1. Enter Play Mode
2. Manually tweak objects
3. Move objects
4. Adjust collider sizes
5. Add scripts/components

### After Play Mode
- The tool lists every manually modified object
- Apply changes per object or apply everything at once

---

## 🖥️ Editor Window UI

### Top Actions
- **Apply All** → applies all recorded changes into Edit Mode  
- **Discard All** → clears all recorded changes

### Object List (Scrollable)
Each modified object includes:
- **Apply**
- **Discard**
- **Ping** (highlight in hierarchy)

The list automatically updates after each Play session.

---

## 📦 Import as Unity Package (Recommended)

For convenience, this repository includes a ready-to-import **Unity Package**, so you can install the tool instantly without manually copying files.

### ✅ Option 1: Import `.unitypackage`
1. Download the included package file:
```

PlaymodeManualApplyTool.unitypackage

```
2. In Unity, go to:
```

Assets → Import Package → Custom Package...

```
3. Select the `.unitypackage` file and click **Import**

This will automatically install everything in the correct folder structure.

---

## 📌 Manual Installation (Alternative)

1. Create folder:

```

Assets/Editor/

```

2. Place the script file:

```

Assets/Editor/PlaymodeManualApplyTool.cs

```

3. Open the tool via:

```

Tools → Playmode Manual Apply

````

---

## 🚀 Usage Guide

### Step-by-step
1. Open the tool window  
2. Enter Play Mode  
3. Modify any object manually  
4. Exit Play Mode  
5. Review the touched object list  
6. Click **Apply** or **Apply All**

---

## 🧪 Demo Script (Optional)

Attach this to a GameObject to test script-field saving:

```csharp
using UnityEngine;

public class DemoPlaymodeChangeScript : MonoBehaviour
{
    public int health = 100;
    public float speed = 5f;
    public string npcName = "Enemy_01";
    public Color tint = Color.white;

    [System.Serializable]
    public class Stats
    {
        public float attack = 10;
        public float defense = 5;
    }

    public Stats stats = new Stats();
}
````

Modify values during Play Mode and apply afterward.

---

## ⚠️ Important Notes

* This tool is designed to record **Undo-based modifications**, meaning:

  * Inspector changes are captured
  * Gizmo edits are captured
  * Add Component actions are captured
* Script-driven runtime movement is ignored (unless a script explicitly uses Undo, which is uncommon)

This makes the tool safe for gameplay simulations where objects constantly move automatically.

---

## 📂 Best Use Cases

* Medical simulations
* Digital twin calibration
* XR interaction tuning
* UI layout adjustments during runtime
* Physics collider tuning
* Rapid iteration workflows

---

## 👨‍💻 Author

**MD. ASAFUDDAULA SOBAHANI SHOUNAK**
Senior Unity Developer & Team Lead
Medical Simulation | Digital Twins | XR (VR/AR) | ECS | Simulation Authoring & Workflow Automation

📍 Dhaka, Bangladesh
📧 Email: [shounak00@gmail.com](mailto:shounak00@gmail.com)
🔗 LinkedIn: [https://www.linkedin.com/in/shounak00](https://www.linkedin.com/in/shounak00)
💻 GitHub: [https://github.com/shounak00](https://github.com/shounak00)
🌐 Portfolio: [https://shounak00.github.io/me](https://shounak00.github.io/me)

---

## 💜 Support / Donation

If this tool helped you and you want to support development, feel free to donate:

**SOL Wallet:**

```
AjaT9TYfBDpuU5avrWhBehUroAvfLctkx7TKHoE3DAkL
```

Thank you for supporting independent Unity tool development 🚀

---

## 📜 License

```
You are free to use, modify, and integrate this tool in personal or commercial Unity projects.
```

