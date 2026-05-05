

# SpotWhy 🔍

**SpotWhy** es un lanzador de aplicaciones y buscador de archivos tipo **macOS Spotlight** para Windows. Aparece al presionar `Ctrl + Espacio` y te permite buscar y abrir aplicaciones, archivos y carpetas al instante.

---

## ✨ Características

- **Búsqueda instantánea** de aplicaciones (Win32 + UWP/Store), archivos y carpetas
- **Activación global** con `Ctrl + Espacio` (como en macOS)
- **Efecto acrílico** (translúcido + blur) nativo de Windows 10/11
- **Tema automático**: se adapta al tema claro/oscuro de Windows
- **Icono verde personalizado** en la bandeja del sistema
- **Tracking de apps más usadas**: las que más abrís aparecen primero
- **Búsqueda inteligente**: soporta acrónimos (`vscode` → Visual Studio Code), palabras parciales y nombres sin espacios
- **Barra de estado** con consumo de memoria en tiempo real
- **Animaciones suaves** al abrir/cerrar y al expandirse los resultados
- **100% en español**
- **Modo portátil**: funciona sin instalación (ejecutar `SpotWhy.exe`)

---

## 🖥️ Tecnologías

| Tecnología | Propósito |
|-----------|-----------|
| **C# / .NET 8** | Lenguaje y runtime principal |
| **WPF (Windows Presentation Foundation)** | UI nativa de Windows con aceleración gráfica |
| **Windows API (P/Invoke)** | Efecto acrílico, hotkey global, esquinas redondeadas |
| **Windows Runtime API** | Detección de apps UWP/Store (Calculadora, etc.) |
| **Everything SDK** | Búsqueda ultrarrápida de archivos (opcional) |
| **Inno Setup 6** | Instalador profesional para Windows |
| **System.Text.Json** | Persistencia del tracking de uso |

---

## 📦 Descargas

| Archivo | Link |
|---------|------|
| **Instalador** (`SpotWhy-Setup-1.0.exe`) | [Descargar](https://github.com/dsantillanAb/Spotwhy/releases/latest) |
| **Versión portátil** (`SpotWhy.exe`) | [Descargar](https://github.com/dsantillanAb/Spotwhy/releases/latest) |

> **Requisito**: Windows 10 2004+ (build 19041) con [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)

---

## 🚀 Instalación

### Opción 1: Instalador (recomendado)

1. Descargá `SpotWhy-Setup-1.0.exe`
2. Ejecutalo y seguí el wizard de instalación
3. Al finalizar, SpotWhy se inicia automáticamente
4. Aparece un icono verde 🔵 en la bandeja del sistema
5. Presioná `Ctrl + Espacio` para abrir el buscador

### Opción 2: Portátil (sin instalación)

1. Descargá la versión portátil
2. Extraé los archivos en cualquier carpeta
3. Ejecutá `SpotWhy.exe`
4. Aparece el icono en la bandeja del sistema

---

## 🎯 Cómo usar

| Acción | Resultado |
|--------|-----------|
| `Ctrl + Espacio` | Abre/cierra el buscador |
| Escribir texto | Busca apps, archivos y carpetas |
| `↓` / `↑` | Navega entre resultados |
| `Enter` | Abre el elemento seleccionado |
| `Escape` | Cierra el buscador |
| `Doble click` | Abre el elemento |
| Click fuera | Cierra automáticamente |

### Búsqueda inteligente

- **"calc"** → encuentra Calculadora, Calculator, etc.
- **"vscode"** → encuentra Visual Studio Code
- **"chrome"** → encuentra Google Chrome, Brave, etc.
- **"doc"** → encuentra Documentos, Word, archivos .docx

---

## 🛠️ Compilar desde código

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Windows 10 2004+ con Windows SDK 10.0.19041
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (solo para generar instalador)

### Pasos

```bash
# Clonar
git clone https://github.com/dsantillanAb/Spotwhy.git
cd Spotwhy

# Compilar
dotnet build SpotlightWindows\SpotlightWindows.csproj -c Release

# Publicar para distribución
dotnet publish SpotlightWindows\SpotlightWindows.csproj -c Release -o publish

# Generar instalador (requiere Inno Setup)
iscc installer.iss
```

---

## 📁 Estructura del proyecto

```
Spotwhy/
├── Spotwhy/                    # Código fuente
│   ├── App.xaml / .cs          # Entry point + bandeja del sistema
│   ├── MainWindow.xaml / .cs   # UI principal tipo Spotlight
│   ├── Models/
│   │   └── SearchResult.cs     # Modelo de datos
│   ├── Services/
│   │   ├── AcrylicService.cs   # Efecto acrílico/blur
│   │   ├── HotkeyService.cs    # Hotkey global Ctrl+Space
│   │   ├── SearchService.cs    # Búsqueda de apps + archivos
│   │   ├── EverythingService.cs# Integración con Everything SDK
│   │   ├── ThemeService.cs     # Tema claro/oscuro del sistema
│   │   └── UsageTracker.cs     # Tracking de apps más usadas
│   └── Converters/
│       ├── TypeToColorConverter.cs    # Colores por tipo
│       └── TypeToSpanishConverter.cs  # Traducción al español
├── app.ico                    # Icono de la aplicación
├── installer.iss              # Script de Inno Setup
├── installer_output/          # Instalador compilado
├── publish/                   # Archivos publicados
├── LICENSE                    # Licencia MIT
└── README.md                  # Este archivo
```

---

## 🔌 Integración con Everything

SpotWhy soporta [Everything](https://www.voidtools.com/) de forma opcional. Si tenés Everything instalado y corriendo, SpotWhy lo detecta automáticamente y lo usa para buscar archivos al instante (mucho más rápido que la búsqueda por directorios).

No requiere configuración — instalá Everything y SpotWhy lo usa automáticamente.

---

## 📄 Licencia

Distribuido bajo licencia MIT. Ver [LICENSE](LICENSE).

---

## 👤 Autor

**Daniel Santillán** — [@dsantillanAb](https://github.com/dsantillanAb)
