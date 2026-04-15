# Matcher 3D

<img width="250" alt="Screenshot_Matcher_2" src="https://github.com/user-attachments/assets/9c21c138-d571-47e7-ab41-b805e1581ec9" />
<img width="250" alt="Screenshot_Matcher_1" src="https://github.com/user-attachments/assets/1185facc-47d3-4d3f-9a55-911a2b231ab5" />
<img width="250" alt="Screenshot_Matcher_3" src="https://github.com/user-attachments/assets/72c4f51a-dfb3-4e62-923c-5267e9f6006f" />

https://github.com/user-attachments/assets/1678b3f4-1c33-41ef-bd56-b3fc070ba9a0


📝 Portfolio Note: This public repository contains the core gameplay systems, scripts, and architecture for Matcher 3D. Art assets, levels, and proprietary plugins have been omitted to comply with Unity Asset Store licensing. The code demonstrates the highly modular systems and Google Sheets metadata pipeline used in the game.

A fun, fast-paced 3D puzzle matching game where players collect, sort, and clear 3D objects to progress through beautiful, themed worlds. Built with a highly modular architecture, a robust metadata pipeline, and optimized for maximum mobile performance.

## 🎮 Gameplay Mechanics

* **Find and Collect:** Tap 3D objects from the main play area to move them into your inventory.
* **Match 3 to Clear:** Your inventory has **7 slots**. Whenever you collect 3 identical items, they automatically match, clear from your slots, and count toward your level goals.
* **Watch Your Space!** If you fill all 7 slots with mismatched items without making a match, you fail the level. Players can utilize **Rewarded Ads** to clean up their slot tray and save their run if they get stuck!
* **Themed Worlds & Progression:** Journey through uniquely stylized map environments. Complete levels to advance along a winding path to the top of the level selection screen. 

## ⚙️ Core Architecture & Best Practices

The game is built with scalability and reusability in mind, utilizing strict separation of concerns:
* **Modular Codebase:** Core systems are entirely decoupled from gameplay logic and UI. The core library can be easily extracted and reused across future projects.
* **Highly Extendable:** Designed to easily accommodate new features, game modes, and mechanics without breaking existing systems.

## 📊 Google Sheets Metadata Pipeline

Virtually every aspect of the game's balance and configuration is driven by a custom remote-config pipeline using Google Sheets. Data is ingested, packaged into the build, and mapped to the `GameSettings` class.

* **Live Ops Ready:** Add new variables directly into the sheet, ingest them, and hook them up in code instantly. Enable or disable specific game items on the fly.
* **Dynamic Level Generation:** Levels are generated using adjustable weights and a difficulty slider. Minimum and maximum resource rewards for each round are entirely configurable via sheets.
* **Economy & Rewards:** In-App Purchases (IAP) configurations, Daily Rewards, and level completion payouts are all defined in the metadata.
* **Ad Configurations:** The frequency, pacing, and specific levels at which Interstitial and Banner ads appear are completely data-driven.
* **Automated Localization:** Fully extendable localization system utilizing Google Sheets' `TRANSLATE` function. Language columns are packaged into localization data and ingested into the game, allowing players to switch languages seamlessly in the settings.

## 📱 Performance & Optimization

Achieving smooth performance on low-end mobile devices was a primary technical pillar:
* **URP Optimization:** Custom graphics settings within the Universal Render Pipeline (URP) specifically tailored to maximize framerates on lower-tier devices.
* **Asset Management:** Powered by **Addressables** for efficient memory loading and unloading.
* **Strict Art Compression:** Texture size limits are strictly applied based on asset screen-space usage. Compression formats are highly optimized per target platform (iOS/Android).

## 🧠 AI-Assisted Development

This project heavily leveraged AI tools to supercharge the development pipeline:
* **Rapid Prototyping:** AI was utilized to quickly generate playable prototypes, allowing for fast iteration on the core "match-3D" loop.
* **Logic Verification:** Cross-checking complex sorting algorithms, slot management, and level generation logic.
* **Boilerplate Generation:** Drastically reduced development time by using AI to write tedious boilerplate code, allowing the focus to remain on architecture and gameplay feel. 

## 🛠️ QA & Debugging Tools

To ensure a highly polished release, the game includes powerful developer tools:
* **Graphics Debug Menu:** Allows developers to tweak rendering settings, shadows, and URP features in real-time on-device to find the perfect balance between visual fidelity and performance.
* **Progression Debugger:** A comprehensive cheat menu used for rapid QA sessions—allowing testers to skip FTUE (First Time User Experience), auto-win levels, fast-forward progression, and grant resources.

## 💰 Player Retention & Monetization

* **FTUE:** A smooth First Time User Experience seamlessly onboards new players into the mechanics.
* **App Review Flow:** Strategically integrated native app review prompts to boost store ratings.
* **Strategic Ads:** Banner ads at specific menus, Interstitial ads between levels, and highly desirable Rewarded Video integrations (e.g., clearing the slot tray to prevent a game over).

---
## 🚀 Getting Started

To run this project locally:

1. Clone the repository:
   ```bash
   git clone [https://github.com/shahzaibjamal/Matcher.git](https://github.com/shahzaibjamal/Matcher.git)

