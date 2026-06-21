*[ [Русский](README.ru.md) ] | [ English ]*

# 🤖 Unity Gemini AI Assistant (Bridge)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity: 2020.3+](https://img.shields.io/badge/Unity-2020.3%2B-black.svg)](https://unity.com/)

**Unity Gemini AI Assistant** is a lightweight Editor plugin that turns Google's Gemini cloud LLM into your personal architectural reviewer directly inside Unity.

This plugin solves the biggest pain point of scaling a Unity project: **you no longer need to manually copy-paste massive walls of code or transcribe Inspector properties** to ask an AI a question. The tool packages your local scene context in a single click.

---

## ✨ Key Features

* 🔍 **Deep Inspector Parsing:** Automatically scans the selected `GameObject`, its tags, layers, hierarchy, and public/serialized values of all attached components.
* 📜 **Automatic Code Ingestion:** Finds the `.cs` files of the scripts attached to the object on your hard drive and injects their raw code into the prompt.
* ⚡ **Zero-Setup / Out-of-the-box:** Written in pure C# (`UnityEngine.Networking`). **No need** to install Python, Node.js, or third-party JSON libraries (like Newtonsoft.Json).
* 🔄 **Smart Model Sync:** Fetches the actual, live list of Gemini models available specifically to your API key directly from Google servers.
* 📋 **Hybrid Export (Clipboard Bridge):** Instantly builds a clean Markdown snapshot of your object and copies it to your clipboard (`Ctrl+C`) for use in the web versions of heavy LLMs (Gemini Advanced, ChatGPT, Claude).

---

## 📦 30-Second Installation

1. Download the `GeminiAssistantWindow.cs` file from this repository.
2. Place it anywhere inside your Unity project inside a folder named **`Editor`** (e.g., `Assets/Editor/GeminiAssistantWindow.cs`).
3. In Unity's top menu, click: **`Tools`** -> **`AI Assistant`**.

---

## ⚙️ Interface & Settings Overview

The plugin window is divided into 4 functional blocks:

### Block 1: AI Settings
* **API Key** — your secret access token. You can get a **free** key in 1 minute at [Google AI Studio](https://aistudio.google.com/). *(The key lives only in your local session and is never saved into your project's code)*.
* **AI Model** — dropdown to select the "brain":
  * `gemini-1.5-flash` — lightning-fast model for quick logic questions (generous free-tier limits).
  * `gemini-1.5-pro` — heavy reasoning model for complex system design.
* **[ Fetch Available Models ] button** — click this once on your first run. The plugin will ping Google and populate the dropdown with only the models that are 100% supported in your tier/region, eliminating `404 Not Found` errors.

### Block 2: Context Automation
* **[x] Analyze Selected Object** — when enabled, the plugin listens to the *Hierarchy* window. Click on any enemy, NPC, or Game Manager on the scene, and the AI will instantly read its composition.
* **[x] Read Attached Scripts Code** — if a `QuestManager.cs` script is attached to the selected object, the plugin will locate the `.cs` file in your project and append its raw source code to the request.

### Block 3: User Request
The text area where you type your question in plain English. For example:
> *"Take a look at this object. Why would the coin spawn logic throw a NullReferenceException when reloading the scene?"*

### Block 4: Action Buttons

1. **`[ Collect Context & Ask AI ]`**  
   Sends the request via the API. Within 3–5 seconds, a detailed answer with syntax highlighting appears in the bottom text area.
2. **`[ Copy Object Context for Chat ]`**  
   *The killer feature for Web Pro/Advanced tier subscribers.* This button costs zero API tokens. It grabs the object's entire "web of context", wraps it in clean Markdown, and sends it to your system clipboard (`Ctrl+C`). Just open your browser and hit `Ctrl+V` in your favorite AI chat.

---

## 🛡️ Security Warning

If you fork or modify this code, **never paste your actual API key into the `private string apiKey = ""` default value** if you plan to push your changes to a public repository. GitHub scrapers will find exposed API keys in minutes.

---

*Developed by a solo engineer*  
**Pull Requests and repository stars ⭐ are highly appreciated!**
