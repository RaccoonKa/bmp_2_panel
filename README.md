# BMP-2 Driver's Instrument Panel Interactive Simulator

## 🎯 Features and Functionality

An interactive training and simulation desktop application built with **WPF (.NET 8)** that replicates the instrument cluster, gauges, switches, indicator lamps, and control elements of the **BMP-2** infantry fighting vehicle (IFV) driver's station:

- **Authentic Gauges & Instrumentation**:
  - **V-1 Ammeter**: Real-time battery charge/discharge monitoring;
  - **Engine Hour Meter**: Accumulated operating time;
  - **TE-45 Tachometer**: Crankshaft rotational speed indication (hundreds of RPM);
  - **Speedometer**: Speed indicator and odometer;
  - **TUE-48 Dual Thermometer**: Coolant (left) and engine oil (right) temperatures;
  - **Oil Pressure Gauge**: Engine lubrication system pressure monitoring;
  - **Transmission Lubricant Pressure Gauge**: Gearbox lubrication loop pressure.
- **Realistic Needle Physics & Animations**:
  - Custom calibrated rotation pivots with smooth needle dynamics using `BackEase` easing curves;
  - Central hubs/caps rendered with accurate perspective and depth layering.
- **Controls & Switches**:
  - Two-position toggle switches with state memory;
  - Push-buttons protected by spring-loaded safety caps (prevents accidental trigger);
  - Rotary dials and rheostats;
  - Instrument night-illumination switch with blackout/dimming overlay effect.
- **Tactical Information Cards**:
  - Interactive contextual information popups (`TacticalInfoCard`) displaying component specifications, operating thresholds, and normal operating envelopes.
- **Operating Modes**:
  - Free Exploration & Learning mode;
  - Standard Engine Start sequence;
  - Engine Start with Auxiliary Pre-lubrication Pump sequence.

---

## 🛠 Tech Stack

- **Platform**: Windows (.NET 8.0)
- **UI Framework**: Windows Presentation Foundation (WPF, XAML)
- **Language**: C# 12
- **Graphics & Assets**: High-resolution photorealistic textures, layered alpha channels, XAML vector shapes, and real-time GPU drop-shadow and glow effects for lamps and instrument backlighting.

---

## 📂 Project Architecture

```text
bmp_2_panel/
├── bmp_2_panel.sln            # Visual Studio Solution
├── global.json                # .NET SDK pinning configuration
├── .gitignore                 # Visual Studio / .NET ignore patterns
├── README.md                  # Project documentation
└── bmp_2_panel/
    ├── App.xaml / .cs         # Application entry point & startup logic
    ├── MainWindow.xaml / .cs  # Main dashboard layout, gauges, and animation handlers
    ├── InfoCard.xaml / .cs    # Contextual tactical info popup component
    ├── AboutWindow.xaml / .cs # "About the Application" modal dialog
    └── assets/                # Asset repository
        ├── background/        # Panel textures, rivets, and frame elements
        ├── buttons/           # Switches, buttons, and protective caps
        ├── designations/      # Military stencil typography and label plates
        ├── lights/            # Signal indicators (active / inactive states)
        └── sensors/           # Dial scales, dials, and calibrated needles
```

---

## 🚀 Building & Running

### Prerequisites
- Windows 10 / 11;
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (x64) or later;
- Visual Studio 2022 (with *.NET desktop development* workload), Visual Studio Code (with C# Dev Kit), or JetBrains Rider.

### Quick Start via CLI

```powershell
# Clone the repository
git clone https://github.com/RaccoonKa/bmp_2_panel.git
cd bmp_2_panel

# Restore and build
dotnet build

# Launch the simulator
dotnet run --project bmp_2_panel/bmp_2_panel.csproj
```

---

## 👥 Authors

- **Lead Developer**: Svetozar Kravchuk
- **Academic Supervisor**: Evgeniy Nikolaevich Rak
