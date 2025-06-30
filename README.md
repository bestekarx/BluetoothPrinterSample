# 🖨️ Bluetooth Printer Sample App

This project is a comprehensive demo application showcasing the usage of the [Maui.Bluetooth.Utils](https://github.com/bestekarx/Maui.Bluetooth.Utils) library. It demonstrates all features of the cross-platform .NET MAUI Bluetooth printer library.

## 📱 Demo Application

![ble](https://raw.githubusercontent.com/bestekarx/BluetoothPrinterSample/refs/heads/main/ex.gif)

*Live demonstration of the Bluetooth Printer Sample App*

## ✨ Features

### 🔧 Core Features
- **Cross-platform** Android and iOS support
- **ESC/POS** protocol support (thermal printers)
- **Zebra ZPL/CPCL** protocol support (label printers)
- **Generic Bluetooth** printer support
- **Dependency Injection** support
- **Event-driven** architecture
- **Async/await** pattern support

### 🚀 Advanced Features
- **Automatic device discovery** and connection management
- **Print job queue** system
- **Printer status monitoring**
- **Connection retry** mechanism
- **Memory leak** protection
- **Thread-safe** operations
- **Error handling** and logging

## 🛠️ Installation

### Requirements
- .NET 8.0 SDK
- Visual Studio 2022 or JetBrains Rider
- Android Studio (for Android development)
- Xcode (for iOS development)

### Steps

1. **Clone the repository**
```bash
git clone https://github.com/bestekarx/Maui.Bluetooth.Utils.git
cd Maui.Bluetooth.Utils/SampleApp/BluetoothPrinterSample
```

2. **Restore NuGet packages**
```bash
dotnet restore
```

3. **Run the application**
```bash
# For Android
dotnet build -t:Run -f net8.0-android

# For iOS
dotnet build -t:Run -f net8.0-ios
```

## 📋 Usage Guide

### 1. Bluetooth Status Check
- Bluetooth status is automatically checked when the app starts
- Use the "🔄 Refresh" button for manual control

### 2. Device Scanning
- Click "🔍 Scan for Devices" to scan for Bluetooth devices
- Activity indicator appears during scanning
- Found devices are displayed in the list

### 3. Device Connection
- Select a device from the list
- Click "🔗 Connect" button
- Connection status is updated in real-time

### 4. Printing Operations

#### 📝 Text Printing
- Enter the text you want to print in the "Text to Print" field
- Click "🖨️ Print Text" button

#### 📊 Barcode Printing
- Enter barcode data in the "Barcode Data" field
- Supported formats: Code128, Code39, EAN-13, EAN-8
- Click "📊 Print Barcode" button

#### 📱 QR Code Printing
- Enter QR code data in the "QR Code Data" field
- Supported formats: URL, text, phone number, email
- Click "📱 Print QR Code" button

#### 🧾 Receipt Printing
- Click "🧾 Print Receipt" to print a sample receipt
- Includes date, time, and total information

#### 🧪 Test Printing
- Click "🧪 Print Test" to print a printer test page
- Tests all printer features

#### ✂️ Paper Cutting
- Click "✂️ Cut Paper" to cut the paper
- Partial or full cut options

### 5. Print Settings
- **X, Y**: Print position
- **Font**: Font number
- **FontSize**: Font size
- **LineHeight**: Line height
- **FeedLines**: Paper feed line count

### 6. Printer Status
- Click "📊 Get Printer Status" to check printer status
- Paper status, connection status, error information

### 7. Activity Log
- All operations are logged in real-time
- Click "🗑️ Clear" to clear the log
- Tap the log area to copy

## 🔧 Technical Details

### Project Structure
```
BluetoothPrinterSample/
├── Platforms/
│   ├── Android/          # Android-specific configurations
│   └── iOS/             # iOS-specific configurations
├── Resources/           # App resources
├── MainPage.xaml        # Main UI
├── MainPage.xaml.cs     # Main logic
├── MauiProgram.cs       # Dependency injection setup
└── BluetoothPrinterSample.csproj
```

### Dependency Injection
```csharp
// MauiProgram.cs
builder.Services.AddBluetoothPrinterServices();
builder.Services.AddSingleton<BluetoothPrinterManager>();
builder.Services.AddTransient<MainPage>();
```

### Used Services
- `IBluetoothService`: Bluetooth connection management
- `IEscPosPrinterService`: ESC/POS printer operations
- `IZebraPrinterService`: Zebra printer operations
- `IGenericPrinterService`: Generic printer operations
- `BluetoothPrinterManager`: Central printer manager

## 🖨️ Supported Printers

### ESC/POS Printers
- **Thermal printers** (receipts, invoices)
- **Cutting capability** (partial/full cut)
- **Paper feed** control
- **Font** and **size** settings
- **Alignment** options
- **Bitmap** printing support

### Zebra Printers
- **Label printers** (ZPL/CPCL)
- **Barcode** printing
- **QR code** printing
- **Custom label** design
- **Printer status** monitoring

### Generic Printers
- **Raw data** transmission
- **Custom commands** support
- **Binary data** printing

## 📱 Platform Support

### Android
- **Minimum API Level**: 21 (Android 5.0)
- **Bluetooth Permissions**: Automatic management
- **Background Operations**: Supported

### iOS
- **Minimum iOS Version**: 11.0
- **Bluetooth Permissions**: Info.plist configuration
- **Background Operations**: Limited support

## 🔒 Security

- **Bluetooth security** is ensured
- **Data encryption** is used
- **Permission handling** is done correctly
- **Secure communication** is provided

## 🐛 Troubleshooting

### Common Issues

1. **Bluetooth is not enabled**
   - Enable Bluetooth from device settings
   - Restart the application

2. **Device not found**
   - Ensure the printer is turned on
   - Check Bluetooth pairing
   - Increase scan duration

3. **Connection failed**
   - Check if the printer is within range
   - Ensure no other app is connected
   - Restart the printer

4. **Printing failed**
   - Check paper status
   - Check printer status
   - Re-establish connection

### Log Analysis
- Check the activity log
- Analyze error messages
- Monitor Bluetooth status

## 🤝 Contributing

To contribute to this demo application:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'feat: Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Guidelines
- Apply **SOLID** principles
- Follow **Clean Code** practices
- Write **unit tests**
- Add **XML documentation**
- Maintain **cross-platform** compatibility

## 📄 License

This project is licensed under the MIT License. See the LICENSE file for details.

## 📞 Contact

- **GitHub**: [@bestekarx](https://github.com/bestekarx)
- **Project**: [Maui.Bluetooth.Utils](https://github.com/bestekarx/Maui.Bluetooth.Utils)

## 🙏 Acknowledgments

This project is inspired by the following open source projects:

- .NET MAUI
- Zebra Link-OS SDK
- ESC/POS Protocol

---

**⭐ If you like this project, don't forget to give it a star! ⭐** 
