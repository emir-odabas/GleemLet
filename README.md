# GleemLet

GleemLet is a WPF desktop application for learning English vocabulary through flashcards, quizzes, and set-based study sessions — with full dark/light theme support.

---

## Features

- **Flashcards** — Study word sets card by card, flip to reveal definitions
- **Quiz Mode** — Test yourself with multiple choice or written answer questions
- **Set Editor** — Create and manage your own custom English word sets
- **Dark / Light Theme** — Switch between themes at any time
- **Sound Feedback** — Audio cues for correct and wrong answers
- **Smooth Animations** — Polished transitions throughout the app

---


## Prerequisites

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Windows 10/11
- Visual Studio 2022 (for development)

---

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/emir-odabas/GleemLet.git
   ```

2. Open `GleemLet.csproj` in Visual Studio 2022.

3. Build and run the project (`F5`).

---

## Build

To publish a self-contained release:
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

---

## Tech Stack

- **C# / WPF** — UI and application logic
- **.NET 8** — Runtime
- **XAML** — Layout and styling

---

## Contributing

Contributions are welcome! Fork the repo and open a pull request.

---

## License

This project is open-source. See the `LICENSE` file for details.
