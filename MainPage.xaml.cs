using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Maui.Bluetooth.Utils.Shared.Interfaces;
using Maui.Bluetooth.Utils.Shared.Models;
using Maui.Bluetooth.Utils.Shared.Services;
using Maui.Bluetooth.Utils.Shared.Utils;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using TextAlignment = Maui.Bluetooth.Utils.Shared.Models.TextAlignment;

namespace BluetoothPrinterSample;

public partial class MainPage : ContentPage, IDisposable
{
    private readonly BluetoothPrinterManager _printerManager;
    private readonly List<BluetoothDeviceModel> _discoveredDevices = new();
    private BluetoothDeviceModel? _connectedDevice;
    private bool _disposed;
    private int _feedLines = 5; // Kullanıcı arayüzünden değiştirilebilir
    private int _barcodeFont = 4;
    private int _barcodeFontSize = 0;
    private bool _barcodeLabelAbove = false;
    private int _qrFont = 4;
    private int _qrFontSize = 0;
    private int _qrLabelYOffset = -60;

    public MainPage(BluetoothPrinterManager printerManager)
    {
        InitializeComponent();
        _printerManager = printerManager ?? throw new ArgumentNullException(nameof(printerManager));

        // Subscribe to events
        _printerManager.ConnectionStateChanged += OnConnectionStateChanged;
        _printerManager.DeviceDiscovered += OnDeviceDiscovered;
        _printerManager.PrintJobStatusChanged += OnPrintJobStatusChanged;
        _printerManager.PrinterStatusChanged += OnPrinterStatusChanged; // Zebra printers

        // Set up device list
        DevicesCollectionView.ItemsSource = _discoveredDevices;

        // Initial Bluetooth check
        _ = CheckBluetoothStatusAsync();
    }

    private async Task CheckBluetoothStatusAsync()
    {
        try
        {
            var isAvailable = await _printerManager.IsBluetoothAvailableAsync();
            BluetoothStatusLabel.Text = isAvailable ? "Available" : "Not Available";
            BluetoothStatusLabel.TextColor = isAvailable ? Colors.Green : Colors.Red;
            
            if (isAvailable)
            {
                var hasPermission = await _printerManager.RequestPermissionsAsync();
                if (!hasPermission)
                {
                    LogMessage("Bluetooth permissions denied");
                    await DisplayAlert("Permission Required", "Bluetooth permissions are required to use this app.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Bluetooth Required", "Please enable Bluetooth to use this app.", "OK");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Error checking Bluetooth: {ex.Message}");
            await DisplayAlert("Error", $"Failed to check Bluetooth status: {ex.Message}", "OK");
        }
    }

    private async void OnCheckBluetoothClicked(object sender, EventArgs e)
    {
        await CheckBluetoothStatusAsync();
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        try
        {
            ScanButton.IsEnabled = false;
            ScanIndicator.IsRunning = true;
            ScanIndicator.IsVisible = true;
            ScanStatusLabel.Text = "Scanning...";
            _discoveredDevices.Clear();
            DevicesCollectionView.ItemsSource = null;

            // Check Bluetooth availability before scanning
            if (!await _printerManager.IsBluetoothAvailableAsync())
            {
                await DisplayAlert("Bluetooth Not Available", "Please enable Bluetooth to scan for devices.", "OK");
                return;
            }

            // Request permissions before scanning
            if (!await _printerManager.RequestPermissionsAsync())
            {
                await DisplayAlert("Permission Required", "Bluetooth permissions are required to scan for devices.", "OK");
                return;
            }

            var devices = await _printerManager.ScanForDevicesAsync();
            
            foreach (var device in devices)
            {
                // Auto-detect printer type if not set
                if (device.PrinterType == PrinterType.Unknown)
                {
                    device.PrinterType = BluetoothPrinterManager.AutoDetectPrinterType(device.Name);
                }
                _discoveredDevices.Add(device);
            }

            DevicesCollectionView.ItemsSource = _discoveredDevices;
            ScanStatusLabel.Text = $"Found {devices.Count} devices";
            LogMessage($"Scan completed. Found {devices.Count} devices.");
            
            if (devices.Count == 0)
            {
                await DisplayAlert("No Devices Found", "No Bluetooth printers were found. Make sure your printer is turned on and in pairing mode.", "OK");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Scan error: {ex.Message}");
            ScanStatusLabel.Text = "Scan failed";
            await DisplayAlert("Scan Error", $"Failed to scan for devices: {ex.Message}", "OK");
        }
        finally
        {
            ScanButton.IsEnabled = true;
            ScanIndicator.IsRunning = false;
            ScanIndicator.IsVisible = false;
        }
    }

    private async void OnDeviceConnectClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is BluetoothDeviceModel device)
        {
            try
            {
                button.IsEnabled = false;
                LogMessage($"Connecting to {device.Name} ({device.PrinterType})...");
                
                var connected = await _printerManager.ConnectAsync(device);
                
                if (connected)
                {
                    _connectedDevice = device;
                    LogMessage($"Connected to {device.Name} ({device.PrinterType})");
                    UpdatePrintButtons(true);
                    UpdatePrinterTypeUI(device.PrinterType);
                    await DisplayAlert("Success", $"Connected to {device.Name} ({device.PrinterType})", "OK");
                }
                else
                {
                    LogMessage($"Failed to connect to {device.Name}");
                    await DisplayAlert("Connection Failed", $"Failed to connect to {device.Name}. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[EXCEPTION] Connection error: {ex.Message}");
                await DisplayAlert("Connection Error", $"Error connecting to device: {ex.Message}", "OK");
            }
            finally
            {
                button.IsEnabled = true;
            }
        }
    }

    private async void OnDisconnectClicked(object sender, EventArgs e)
    {
        try
        {
            DisconnectButton.IsEnabled = false;
            await _printerManager.DisconnectAsync();
            _connectedDevice = null;
            UpdatePrintButtons(false);
            UpdatePrinterTypeUI(PrinterType.Unknown);
            LogMessage("Disconnected");
            await DisplayAlert("Disconnected", "Successfully disconnected from printer.", "OK");
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Disconnect error: {ex.Message}");
            await DisplayAlert("Disconnect Error", $"Error disconnecting: {ex.Message}", "OK");
        }
        finally
        {
            DisconnectButton.IsEnabled = true;
        }
    }

    private int GetIntFromEntry(Entry entry, int defaultValue)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.Text)) return defaultValue;
        if (int.TryParse(entry.Text, out int value)) return value;
        return defaultValue;
    }

    private async void OnPrintTextClicked(object sender, EventArgs e)
    {
        if (_connectedDevice == null) return;
        try
        {
            PrintTextButton.IsEnabled = false;
            var text = PrintTextEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Input Required", "Please enter text to print.", "OK");
                return;
            }
            int x = GetIntFromEntry(XEntry, 30);
            int y = GetIntFromEntry(YEntry, 40);
            int font = GetIntFromEntry(FontEntry, 4);
            int fontSize = GetIntFromEntry(FontSizeEntry, 0);
            int feedLines = GetIntFromEntry(FeedLinesEntry, 5);
            bool success;
            if (_connectedDevice.PrinterType == PrinterType.Zebra)
            {
                success = await _printerManager.PrintZebraTextLabelAsync(text, x, y, font, fontSize, feedLines);
            }
            else
            {
                success = await _printerManager.PrintTextAsync(text, TextAlignment.Center, true);
            }
            if (success)
            {
                LogMessage($"Text printed successfully: {text}");
                await DisplayAlert("Success", "Text printed successfully!", "OK");
            }
            else
            {
                LogMessage("Failed to print text");
                await DisplayAlert("Print Error", "Failed to print text. Please check printer connection.", "OK");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Print text error: {ex.Message}");
            await DisplayAlert("Print Error", $"Error printing text: {ex.Message}", "OK");
        }
        finally
        {
            PrintTextButton.IsEnabled = true;
        }
    }

    private async void OnPrintBarcodeClicked(object sender, EventArgs e)
    {
        if (_connectedDevice == null) return;
        
        var barcodeData = BarcodeEntry.Text?.Trim();
        if (string.IsNullOrEmpty(barcodeData))
        {
            await DisplayAlert("Hata", "Barcode verisi giriniz", "Tamam");
            return;
        }

        try
        {
            PrintBarcodeButton.IsEnabled = false;
            
            bool success = false;
            
            if (_connectedDevice.PrinterType == PrinterType.Zebra)
            {
                // Zebra printer için ZPL kullan
                int x = GetIntFromEntry(XEntry, 50);
                int y = GetIntFromEntry(YEntry, 100);
                int font = GetIntFromEntry(FontEntry, 4);
                int fontSize = GetIntFromEntry(FontSizeEntry, 0);
                int feedLines = GetIntFromEntry(FeedLinesEntry, 5);
                bool labelAbove = _barcodeLabelAbove;
                
                success = await _printerManager.PrintZebraBarcodeLabelAsync(barcodeData, $"Barcode: {barcodeData}", x, y, 128, 50, 1, font, fontSize, labelAbove, feedLines);
            }
            else
            {
                // ESC/POS printer için yeni metodu kullan
                success = await _printerManager.PrintSimpleBarcodeAsync(barcodeData, BarcodeType.Code128);
            }

            if (success)
            {
                LogMessage($"Barcode printed successfully: {barcodeData}");
                await DisplayAlert("Başarılı", "Barcode yazdırıldı", "Tamam");
            }
            else
            {
                LogMessage("Failed to print barcode");
                await DisplayAlert("Hata", "Barcode yazdırılamadı", "Tamam");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Print barcode error: {ex.Message}");
            await DisplayAlert("Hata", $"Barcode yazdırma hatası: {ex.Message}", "Tamam");
        }
        finally
        {
            PrintBarcodeButton.IsEnabled = true;
        }
    }

    private async void OnPrintQrCodeClicked(object sender, EventArgs e)
    {
        if (_connectedDevice == null) return;
        
        var qrData = QrCodeEntry.Text?.Trim();
        if (string.IsNullOrEmpty(qrData))
        {
            await DisplayAlert("Hata", "QR kod verisi giriniz", "Tamam");
            return;
        }

        try
        {
            PrintQrCodeButton.IsEnabled = false;
            
            bool success = false;
            
            if (_connectedDevice.PrinterType == PrinterType.Zebra)
            {
                // Zebra printer için ZPL kullan
                int x = GetIntFromEntry(XEntry, 50);
                int y = GetIntFromEntry(YEntry, 100);
                int font = GetIntFromEntry(FontEntry, 4);
                int fontSize = GetIntFromEntry(FontSizeEntry, 0);
                int feedLines = GetIntFromEntry(FeedLinesEntry, 5);
                int labelYOffset = -60;
                
                success = await _printerManager.PrintZebraQrLabelAsync(qrData, $"QR Code: {qrData}", x, y, 2, "M", font, fontSize, labelYOffset, feedLines);
            }
            else
            {
                // ESC/POS printer için yeni metodları dene
                // Önce basit metodu dene
                success = await _printerManager.PrintQrCodeVerySimpleAsync(qrData, 4);
                
                if (!success)
                {
                    // Basit metot başarısız olursa alternatif metodu dene
                    success = await _printerManager.PrintQrCodeAlternativeAsync(qrData, 6);
                }
                
                if (!success)
                {
                    // Son olarak normal metodu dene
                    success = await _printerManager.PrintQrCodeAsync(qrData, 6);
                }
            }

            if (success)
            {
                LogMessage($"QR code printed successfully: {qrData}");
                await DisplayAlert("Başarılı", "QR kod yazdırıldı", "Tamam");
            }
            else
            {
                LogMessage("Failed to print QR code");
                await DisplayAlert("Hata", "QR kod yazdırılamadı", "Tamam");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Print QR code error: {ex.Message}");
            await DisplayAlert("Hata", $"QR kod yazdırma hatası: {ex.Message}", "Tamam");
        }
        finally
        {
            PrintQrCodeButton.IsEnabled = true;
        }
    }

    private async void OnPrintReceiptClicked(object sender, EventArgs e)
    {
        if (_connectedDevice == null) return;

        try
        {
            PrintReceiptButton.IsEnabled = false;

            if (_connectedDevice.PrinterType == PrinterType.Zebra)
            {
                // Zebra printer - create receipt-style label
                var receiptLabel = GenerateZebraReceiptLabel();
                var success = await _printerManager.PrintZplLabelAsync(receiptLabel);
                
                if (success)
                {
                    LogMessage("Zebra receipt label printed successfully");
                    await DisplayAlert("Success", "Receipt label printed successfully!", "OK");
                }
                else
                {
                    LogMessage("Failed to print Zebra receipt label");
                    await DisplayAlert("Print Error", "Failed to print receipt label.", "OK");
                }
            }
            else
            {
                // ESC/POS printer - print receipt
                var printData = new List<PrintDataModel>
                {
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "=== RECEIPT ===",
                        Alignment = TextAlignment.Center,
                        IsBold = true,
                        FontSize = 16
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        Alignment = TextAlignment.Left,
                        FontSize = 12
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Item 1: Sample Product 1",
                        Alignment = TextAlignment.Left
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Price: $10.00",
                        Alignment = TextAlignment.Right
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Item 2: Sample Product 2",
                        Alignment = TextAlignment.Left
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Price: $15.50",
                        Alignment = TextAlignment.Right
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Total: $25.50",
                        Alignment = TextAlignment.Right,
                        IsBold = true,
                        FontSize = 14
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Thank you for your purchase!",
                        Alignment = TextAlignment.Center,
                        FontSize = 12
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Receipt ID: 12345",
                        Alignment = TextAlignment.Center,
                        FontSize = 10
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Barcode,
                        Content = "12345",
                        BarcodeType = BarcodeType.Code128
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Text,
                        Content = "Scan for online receipt",
                        Alignment = TextAlignment.Center,
                        FontSize = 10
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.QrCode,
                        Content = "https://receipt.example.com/12345",
                        QrErrorLevel = QrCodeErrorLevel.M
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.LineBreak,
                        Content = ""
                    },
                    new PrintDataModel
                    {
                        Type = PrintDataType.Cut,
                        Content = ""
                    }
                };

                var success = await _printerManager.PrintDataAsync(printData);
                
                if (success)
                {
                    LogMessage("Receipt printed successfully");
                    await DisplayAlert("Success", "Receipt printed successfully!", "OK");
                }
                else
                {
                    LogMessage("Failed to print receipt");
                    await DisplayAlert("Print Error", "Failed to print receipt. Please check printer connection.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Print receipt error: {ex.Message}");
            await DisplayAlert("Print Error", $"Error printing receipt: {ex.Message}", "OK");
        }
        finally
        {
            PrintReceiptButton.IsEnabled = true;
        }
    }

    private async void OnPrintTestLabelClicked(object sender, EventArgs e)
    {
        if (_connectedDevice == null) return;

        try
        {
            PrintTestLabelButton.IsEnabled = false;

            if (_connectedDevice.PrinterType == PrinterType.Zebra)
            {
                var success = await _printerManager.PrintZebraTestLabelAsync();
                
                if (success)
                {
                    LogMessage("Zebra test label printed successfully");
                    await DisplayAlert("Success", "Test label printed successfully!", "OK");
                }
                else
                {
                    LogMessage("Failed to print Zebra test label");
                    await DisplayAlert("Print Error", "Failed to print test label.", "OK");
                }
            }
            else
            {
                // ESC/POS test print
                var success = await _printerManager.PrintTextAsync("=== TEST PRINT ===", TextAlignment.Center, true);
                if (success)
                {
                    await _printerManager.PrintTextAsync($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", TextAlignment.Center);
                    await _printerManager.PrintTextAsync("This is a test print", TextAlignment.Center);
                    await _printerManager.PrintLineBreakAsync(2);
                    
                    // Test barcode
                    await _printerManager.PrintTextAsync("Test Barcode:", TextAlignment.Center);
                    await _printerManager.PrintSimpleBarcodeAsync("123456789", BarcodeType.Code128);
                    await _printerManager.PrintLineBreakAsync(2);
                    
                    // Test QR code - try different methods
                    await _printerManager.PrintTextAsync("Test QR Code:", TextAlignment.Center);
                    
                    // Try very simple method first
                    var qrSuccess = await _printerManager.PrintQrCodeVerySimpleAsync("https://www.example.com", 4);
                    
                    if (!qrSuccess)
                    {
                        // Try alternative method
                        qrSuccess = await _printerManager.PrintQrCodeAlternativeAsync("https://www.example.com", 6);
                    }
                    
                    if (!qrSuccess)
                    {
                        // Try regular method
                        qrSuccess = await _printerManager.PrintQrCodeAsync("https://www.example.com", 6);
                    }
                    
                    await _printerManager.PrintLineBreakAsync(2);
                    
                    await _printerManager.PrintTextAsync("Test completed!", TextAlignment.Center, true);
                    await _printerManager.PrintLineBreakAsync(3);
                    await _printerManager.CutPaperAsync();
                    
                    LogMessage("Test print completed successfully");
                    await DisplayAlert("Success", "Test print completed successfully!", "OK");
                }
                else
                {
                    LogMessage("Failed to print test");
                    await DisplayAlert("Print Error", "Failed to print test.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Test print error: {ex.Message}");
            await DisplayAlert("Test Print Error", $"Error printing test: {ex.Message}", "OK");
        }
        finally
        {
            PrintTestLabelButton.IsEnabled = true;
        }
    }

    private async void OnCutPaperClicked(object sender, EventArgs e)
    {
        if (_connectedDevice == null) return;

        try
        {
            CutPaperButton.IsEnabled = false;
            
            if (_connectedDevice.PrinterType == PrinterType.Zebra)
            {
                // Zebra printers don't have cut paper, but we can print a blank label
                var blankLabel = "^XA^XZ";
                var success = await _printerManager.PrintZplLabelAsync(blankLabel);
                
                if (success)
                {
                    LogMessage("Zebra label advanced");
                    await DisplayAlert("Success", "Label advanced successfully!", "OK");
                }
                else
                {
                    LogMessage("Failed to advance Zebra label");
                    await DisplayAlert("Error", "Failed to advance label.", "OK");
                }
            }
            else
            {
                // ESC/POS printer
                var success = await _printerManager.CutPaperAsync();
                
                if (success)
                {
                    LogMessage("Paper cut successfully");
                    await DisplayAlert("Success", "Paper cut successfully!", "OK");
                }
                else
                {
                    LogMessage("Failed to cut paper");
                    await DisplayAlert("Error", "Failed to cut paper.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Cut paper error: {ex.Message}");
            await DisplayAlert("Cut Paper Error", $"Error cutting paper: {ex.Message}", "OK");
        }
        finally
        {
            CutPaperButton.IsEnabled = true;
        }
    }

    private async void OnGetPrinterStatusClicked(object sender, EventArgs e)
    {
        if (_connectedDevice == null) return;

        try
        {
            GetStatusButton.IsEnabled = false;

            if (_connectedDevice.PrinterType == PrinterType.Zebra)
            {
                var status = await _printerManager.GetZebraPrinterStatusAsync();
                var isReady = await _printerManager.IsZebraPrinterReadyAsync();
                
                var statusMessage = $"Zebra Printer Status:\n" +
                                   $"Ready: {isReady}\n" +
                                   $"Paused: {status.IsPaused}\n" +
                                   $"Printing: {status.IsPrinting}\n" +
                                   $"Waiting for Label: {status.IsWaitingForLabel}\n" +
                                   $"Waiting for Ribbon: {status.IsWaitingForRibbon}\n" +
                                   $"Waiting for Media: {status.IsWaitingForMedia}\n" +
                                   $"Error: {status.ErrorMessage ?? "None"}";
                
                LogMessage($"Printer status: {statusMessage}");
                await DisplayAlert("Printer Status", statusMessage, "OK");
            }
            else
            {
                var status = await _printerManager.GetPrinterStatusAsync();
                var statusMessage = $"ESC/POS Printer Status:\n" +
                                   $"Status: {status}";
                
                LogMessage($"Printer status: {statusMessage}");
                await DisplayAlert("Printer Status", statusMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Get status error: {ex.Message}");
            await DisplayAlert("Status Error", $"Error getting printer status: {ex.Message}", "OK");
        }
        finally
        {
            GetStatusButton.IsEnabled = true;
        }
    }

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        LogEditor.Text = string.Empty;
    }

    private async void OnLogEditorTapped(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(LogEditor.Text))
            {
                await Clipboard.SetTextAsync(LogEditor.Text);
                LogMessage("📋 Activity log copied to clipboard");
                await DisplayAlert("Copied", "Activity log has been copied to clipboard", "OK");
            }
            else
            {
                await DisplayAlert("Empty Log", "No activity log to copy", "OK");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[EXCEPTION] Failed to copy log: {ex.Message}");
            await DisplayAlert("Copy Error", $"Failed to copy log to clipboard: {ex.Message}", "OK");
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var stateText = e.CurrentState switch
            {
                ConnectionState.Disconnected => "Disconnected",
                ConnectionState.Connecting => "Connecting...",
                ConnectionState.Connected => "Connected",
                ConnectionState.Failed => "Connection Failed",
                _ => "Unknown"
            };

            ConnectionStatusLabel.Text = stateText;
            ConnectionStatusLabel.TextColor = e.CurrentState == ConnectionState.Connected ? Colors.Green : Colors.Red;

            if (e.Device != null)
            {
                ConnectedDeviceLabel.Text = $"{e.Device.Name} ({e.Device.PrinterType})";
            }

            LogMessage($"Connection state changed: {e.PreviousState} -> {e.CurrentState}");
            
            if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                LogMessage($"Connection error: {e.ErrorMessage}");
            }
        });
    }

    private void OnDeviceDiscovered(object? sender, BluetoothDeviceModel device)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogMessage($"Device discovered: {device.Name} ({device.Address})");
        });
    }

    private void OnPrintJobStatusChanged(object? sender, PrintJobStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogMessage($"Print job {e.JobId}: {e.Status} ({e.Progress}%)");
        });
    }

    private void OnPrinterStatusChanged(object? sender, PrinterStatusChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogMessage($"Zebra printer status changed: Ready={e.CurrentStatus.IsReady}, Error={e.CurrentStatus.ErrorMessage ?? "None"}");
            
            if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                LogMessage($"Zebra printer error: {e.ErrorMessage}");
            }
        });
    }

    private void UpdatePrintButtons(bool enabled)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PrintTextButton.IsEnabled = enabled;
            PrintBarcodeButton.IsEnabled = enabled;
            PrintQrCodeButton.IsEnabled = enabled;
            PrintReceiptButton.IsEnabled = enabled;
            PrintTestLabelButton.IsEnabled = enabled;
            CutPaperButton.IsEnabled = enabled;
            GetStatusButton.IsEnabled = enabled;
            DisconnectButton.IsEnabled = enabled;
        });
    }

    private void UpdatePrinterTypeUI(PrinterType printerType)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var isZebra = printerType == PrinterType.Zebra;
            var isEscPos = printerType == PrinterType.EscPos || printerType == PrinterType.Generic;
            
            // Update button texts for Zebra printers
            if (isZebra)
            {
                PrintTextButton.Text = "🖨️ Print Text Label";
                PrintBarcodeButton.Text = "📊 Print Barcode Label";
                PrintQrCodeButton.Text = "📱 Print QR Label";
                PrintReceiptButton.Text = "🧾 Print Receipt Label";
                PrintTestLabelButton.Text = "🧪 Print Test Label";
                CutPaperButton.Text = "⏭️ Advance Label";
            }
            else
            {
                PrintTextButton.Text = "🖨️ Print Text";
                PrintBarcodeButton.Text = "📊 Print Barcode";
                PrintQrCodeButton.Text = "📱 Print QR Code";
                PrintReceiptButton.Text = "🧾 Print Receipt";
                PrintTestLabelButton.Text = "🧪 Print Test";
                CutPaperButton.Text = "✂️ Cut Paper";
            }
        });
    }

    private void LogMessage(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogEditor.Text += $"[{timestamp}] {message}\n";
            LogEditor.CursorPosition = LogEditor.Text.Length;
        });
    }

    private string GenerateZebraReceiptLabel()
    {
        return ZebraUtils.GenerateLabel(
            ZebraUtils.Text("=== RECEIPT ===", 50, 50, "0", 40),
            ZebraUtils.Text($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", 50, 100, "0", 25),
            ZebraUtils.Text("Item 1: Sample Product 1", 50, 140, "0", 25),
            ZebraUtils.Text("Price: $10.00", 300, 140, "0", 25),
            ZebraUtils.Text("Item 2: Sample Product 2", 50, 170, "0", 25),
            ZebraUtils.Text("Price: $15.50", 300, 170, "0", 25),
            ZebraUtils.Line(50, 200, 350, 200, 2),
            ZebraUtils.Text("Total: $25.50", 300, 230, "0", 30),
            ZebraUtils.Text("Thank you for your purchase!", 50, 270, "0", 25),
            ZebraUtils.Rectangle(40, 40, 360, 280, 2)
        );
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_printerManager != null)
            {
                _printerManager.ConnectionStateChanged -= OnConnectionStateChanged;
                _printerManager.DeviceDiscovered -= OnDeviceDiscovered;
                _printerManager.PrintJobStatusChanged -= OnPrintJobStatusChanged;
                _printerManager.PrinterStatusChanged -= OnPrinterStatusChanged;
            }
            _disposed = true;
        }
    }
} 