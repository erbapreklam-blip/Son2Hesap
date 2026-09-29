using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace ReklamHesap
{
    public class MainForm : Form
    {
        private readonly Dictionary<string, NumericUpDown> inputs = new();
        private readonly Dictionary<string, (RadioButton Var, RadioButton Yok)> toggles = new();
        private RadioButton laminasyonVar;
        private RadioButton laminasyonYok;
        private NumericUpDown servisKm;
        private Label totalLabel;
        private Panel contentPanel;
        private bool kdv20Aktif;
        private int karYuzde = 0; // 0=kazanç yok, 50=%50, 75=%75, 100=%100

        private readonly Color topColor = Color.FromArgb(32, 32, 32);
        private readonly Color activeColor = Color.FromArgb(46, 160, 67);
        private readonly Color backgroundColor = Color.FromArgb(245, 245, 245);

        public MainForm()
        {
            Database.Initialize();
            Text = "Reklam Hesap";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1100, 820);
            MinimumSize = new Size(900, 650);
            Font = new Font("Segoe UI", 10F);
            BackColor = backgroundColor;

            BuildTopMenu();
            ShowHome();
        }

        private void BuildTopMenu()
        {
            var top = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = topColor,
                Padding = new Padding(10, 8, 10, 8)
            };

            var menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                BackColor = topColor,
                Padding = new Padding(0)
            };

            menu.Controls.Add(MenuButton("Ana Sayfa", true, (s, e) => ShowHome()));
            menu.Controls.Add(MenuButton("Ürün Ekleme", false, (s, e) => ShowProductInfo()));
            menu.Controls.Add(MenuButton("Fiyat Düzenle", false, (s, e) => ShowPriceEdit()));
            menu.Controls.Add(MenuButton("KDV", false, (s, e) => ShowKdv()));
            menu.Controls.Add(MenuButton("KAR", false, (s, e) => ShowProfit()));
            menu.Controls.Add(MenuButton("Yedek Al / Geri Yükle", false, (s, e) => ShowBackupInfo(), 210));

            top.Controls.Add(menu);
            Controls.Add(top);
        }

        private Button MenuButton(string text, bool active, EventHandler click, int width = 150)
        {
            var b = new Button
            {
                Text = text,
                Width = width,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? activeColor : topColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F),
                Margin = new Padding(4, 0, 4, 0),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += click;
            return b;
        }

        private void ShowHome()
        {
            Controls.Remove(contentPanel);
            contentPanel?.Dispose();
            inputs.Clear();
            toggles.Clear();

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = backgroundColor,
                Padding = new Padding(25, 20, 25, 30)
            };

            var wrapper = new Panel
            {
                Width = 780,
                AutoSize = true,
                BackColor = Color.White,
                Padding = new Padding(22, 18, 22, 25)
            };

            var title = new Label
            {
                Text = "HESAPLAMA",
                Font = new Font("Segoe UI Semibold", 18F),
                ForeColor = Color.FromArgb(35, 35, 35),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 15)
            };

            var table = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                ColumnCount = 3,
                RowCount = 0,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));

            AddHeader(table, "ÜRÜN / HİZMET", "MİKTAR", "BİRİM");

            AddToggleRow(table, "Laminasyon", out laminasyonVar, out laminasyonYok, "Var", "Yok");
            AddInputRow(table, "Kumlama Folyo", "M2");
            AddInputRow(table, "Kumlama Baskılı Folyo", "M2");
            AddInputRow(table, "Gold Folyo", "M2");
            AddInputRow(table, "Gümüş Folyo", "M2");
            AddInputRow(table, "Şeffaf Folyo", "M2");
            AddInputRow(table, "Simli Folyo", "M2");
            AddInputRow(table, "Desenli Folyo", "M2");
            AddInputRow(table, "Baskılı Folyo", "M2");
            AddInputRow(table, "Kesim Folyosu", "M2");
            AddInputRow(table, "Plotter Kesim", "M2");
            AddInputRow(table, "Bas-Kes", "M2");
            AddInputRow(table, "Cam Kirli / Cam Temizliği", "M2");
            AddInputRow(table, "Servis KM Gidiş-Dönüş", "KM", out servisKm);
            AddToggleRow(table, "Yemek", out _, out _, "Var", "Yok");
            AddToggleRow(table, "İskele", out _, out _, "Var", "Yok");
            AddToggleRow(table, "Merdiven", out _, out _, "Var", "Yok");
            AddToggleRow(table, "Vinç", out _, out _, "Var", "Yok");

            AddInputRow(table, "Kompozit 3mm", "M2");
            AddInputRow(table, "Kompozit 4.5mm", "M2");
            AddInputRow(table, "Polikarbon", "M2");
            AddInputRow(table, "Dekote 4.5mm", "M2");
            AddInputRow(table, "Dekote 2.5mm", "M2");
            AddInputRow(table, "Dekote 8mm", "M2");
            AddInputRow(table, "Şeffaf P. 6mm", "M2");
            AddInputRow(table, "Şerit Led", "M2");
            AddInputRow(table, "Köşebent 20x20", "M2");
            AddInputRow(table, "Köşebent 30x30", "M2");

            wrapper.Controls.Add(title);
            wrapper.Controls.Add(table);

            var calcPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 75,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 18, 0, 0),
                BackColor = Color.White
            };

            var calculate = new Button
            {
                Text = "",
                Width = 180,
                Height = 45,
                BackColor = activeColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 12F),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 15, 0)
            };
            calculate.FlatAppearance.BorderSize = 0;
            calculate.Click += (s, e) => CalculateTotal();

            totalLabel = new Label
            {
                Text = "0,00 TL",
                Width = 270,
                Height = 45,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250),
                Font = new Font("Segoe UI Semibold", 14F),
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(0)
            };

            calcPanel.Controls.Add(calculate);
            calcPanel.Controls.Add(totalLabel);
            wrapper.Controls.Add(calcPanel);

            contentPanel.Controls.Add(wrapper);
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
        }

        private void AddHeader(TableLayoutPanel table, string a, string b, string c)
        {
            AddCell(table, new Label
            {
                Text = a,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = Color.DimGray,
                Padding = new Padding(10, 0, 5, 0),
                Height = 34
            }, 0, 0);
            AddCell(table, new Label
            {
                Text = b,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = Color.DimGray,
                Height = 34
            }, 1, 0);
            AddCell(table, new Label
            {
                Text = c,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 9F),
                ForeColor = Color.DimGray,
                Height = 34
            }, 2, 0);
        }

        private void AddInputRow(TableLayoutPanel table, string name, string unit)
        {
            AddInputRow(table, name, unit, out _);
        }

        private void AddInputRow(TableLayoutPanel table, string name, string unit, out NumericUpDown control)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            var label = new Label
            {
                Text = name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F),
                Padding = new Padding(10, 0, 5, 0)
            };

            control = new NumericUpDown
            {
                DecimalPlaces = 2,
                Minimum = 0,
                Maximum = 1000000,
                Increment = 0.5M,
                Width = 150,
                Height = 30,
                Anchor = AnchorStyles.None,
                TextAlign = HorizontalAlignment.Right,
                Font = new Font("Segoe UI", 10F)
            };
            inputs[name] = control;

            var unitLabel = new Label
            {
                Text = unit,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };

            AddCell(table, label, 0, row);
            AddCell(table, control, 1, row);
            AddCell(table, unitLabel, 2, row);
        }

        private void AddToggleRow(TableLayoutPanel table, string name, out RadioButton varButton, out RadioButton yokButton, string varText, string yokText)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            var label = new Label
            {
                Text = name,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F),
                Padding = new Padding(10, 0, 5, 0)
            };

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.None,
                AutoSize = false,
                Padding = new Padding(15, 12, 0, 0)
            };

            varButton = new RadioButton { Text = varText, AutoSize = true, Margin = new Padding(0, 0, 18, 0) };
            yokButton = new RadioButton { Text = yokText, AutoSize = true };
            yokButton.Checked = true;
            panel.Controls.Add(varButton);
            panel.Controls.Add(yokButton);

            toggles[name] = (varButton, yokButton);

            var unitLabel = new Label { Text = "", Dock = DockStyle.Fill };
            AddCell(table, label, 0, row);
            AddCell(table, panel, 1, row);
            AddCell(table, unitLabel, 2, row);
        }

        private void AddCell(TableLayoutPanel table, Control control, int column, int row)
        {
            table.Controls.Add(control, column, row);
        }

        private void CalculateTotal()
        {
            decimal total = 0;
            var products = Database.Products() ?? new List<ProductRow>();
            var services = Database.Services() ?? new List<ServiceRow>();

            decimal Price(string name, string sub = null)
            {
                var p = products.FirstOrDefault(x =>
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.SubOption ?? "", sub ?? "", StringComparison.OrdinalIgnoreCase));
                return p?.Price ?? 0m;
            }

            decimal ServicePrice(string name)
            {
                var s = services.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                return s?.Price ?? 0m;
            }

            foreach (var item in inputs)
            {
                decimal quantity = item.Value.Value;
                if (quantity <= 0) continue;
                total += quantity * Price(item.Key);
            }

            // Laminasyon is an add-on to the Baskılı Folyo area.
            if (laminasyonVar != null && laminasyonVar.Checked)
            {
                decimal baskiliM2 = inputs.TryGetValue("Baskılı Folyo", out var b) ? b.Value : 0m;
                total += baskiliM2 * Price("Laminasyon", "Var");
            }

            if (servisKm != null && servisKm.Value > 0)
                total += servisKm.Value * ServicePrice("Servis");

            AddToggleService(ref total, "Yemek", ServicePrice("Yemek"));
            AddToggleService(ref total, "İskele", ServicePrice("İskele"));
            AddToggleService(ref total, "Merdiven", ServicePrice("Merdiven"));
            AddToggleService(ref total, "Vinç", ServicePrice("Vinç"));

            // Önce maliyetin üzerine seçilen kazancı ekle.
            decimal profitMultiplier = karYuzde == 100 ? 2.00m :
                                       karYuzde == 75 ? 1.75m :
                                       karYuzde == 50 ? 1.50m : 1.00m;
            decimal saleTotal = total * profitMultiplier;

            // KDV, kazanç eklenmiş satış tutarı üzerinden uygulanır.
            decimal finalTotal = kdv20Aktif ? saleTotal * 1.20m : saleTotal;
            totalLabel.Text = finalTotal.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " TL";
        }

        private void AddToggleService(ref decimal total, string name, decimal price)
        {
            if (toggles.TryGetValue(name, out var t) && t.Var.Checked)
                total += price;
        }

        private void ShowPriceEdit()
        {
            Controls.Remove(contentPanel);
            contentPanel?.Dispose();

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = backgroundColor,
                Padding = new Padding(25, 20, 25, 30)
            };

            var wrapper = new Panel
            {
                Width = 820,
                AutoSize = true,
                BackColor = Color.White,
                Padding = new Padding(22, 18, 22, 25)
            };

            var title = new Label
            {
                Text = "FİYAT DÜZENLE",
                Font = new Font("Segoe UI Semibold", 18F),
                ForeColor = Color.FromArgb(35, 35, 35),
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 42
            };
            wrapper.Controls.Add(title);

            var table = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 0,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.White
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));

            AddPriceHeader(table, "ÜRÜN / HİZMET", "SEÇENEK", "BİRİM", "FİYAT (TL)");

            var productRows = new[]
            {
                ("Laminasyon", "Laminasyonlu", "m2", "Var"),
                ("Laminasyon", "Laminasyonsuz", "m2", "Yok"),
                ("Kumlama Folyo", "", "m2", ""),
                ("Kumlama Baskılı Folyo", "", "m2", ""),
                ("Gold Folyo", "", "m2", ""),
                ("Gümüş Folyo", "", "m2", ""),
                ("Şeffaf Folyo", "", "m2", ""),
                ("Simli Folyo", "", "m2", ""),
                ("Desenli Folyo", "", "m2", ""),
                ("Baskılı Folyo", "", "m2", ""),
                ("Kesim Folyosu", "", "m2", ""),
                ("Plotter Kesim", "", "m2", ""),
                ("Bas-Kes", "", "m2", ""),
                ("Cam Kirli / Cam Temizliği", "", "m2", ""),
                ("Kompozit 3mm", "", "m2", ""),
                ("Kompozit 4.5mm", "", "m2", ""),
                ("Polikarbon", "", "m2", ""),
                ("Dekote 4.5mm", "", "m2", ""),
                ("Dekote 2.5mm", "", "m2", ""),
                ("Dekote 8mm", "", "m2", ""),
                ("Şeffaf P. 6mm", "", "m2", ""),
                ("Şerit Led", "", "m2", ""),
                ("Köşebent 20x20", "", "m2", ""),
                ("Köşebent 30x30", "", "m2", "")
            };

            var products = Database.Products();
            foreach (var item in productRows)
            {
                string sub = item.Item4;
                var found = products.FirstOrDefault(x =>
                    string.Equals(x.Name, item.Item1, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.SubOption ?? "", sub, StringComparison.OrdinalIgnoreCase));
                AddPriceRow(table, item.Item1, item.Item2, item.Item3, found?.Price ?? 0m,
                    price =>
                    {
                        if (found != null)
                            Database.SetProductPrice(found.Id, price);
                    });
            }

            var services = Database.Services();
            var serviceRows = new[]
            {
                ("Servis KM Gidiş-Dönüş", "Servis", "km"),
                ("Yemek", "", "kişi"),
                ("İskele", "", "iş"),
                ("Merdiven", "", "iş"),
                ("Vinç", "", "iş")
            };

            foreach (var item in serviceRows)
            {
                var found = services.FirstOrDefault(x => string.Equals(x.Name, item.Item2.Length == 0 ? item.Item1 : item.Item2, StringComparison.OrdinalIgnoreCase));
                AddPriceRow(table, item.Item1, "", item.Item3, found?.Price ?? 0m,
                    price =>
                    {
                        if (found != null)
                            Database.SetServicePrice(found.Id, price);
                    });
            }

            wrapper.Controls.Add(table);

            var save = new Button
            {
                Text = "FİYATLARI KAYDET",
                Width = 220,
                Height = 48,
                BackColor = activeColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11F),
                Margin = new Padding(0, 18, 0, 0),
                Cursor = Cursors.Hand
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += (s, e) =>
            {
                MessageBox.Show("Fiyatlar kaydedildi.", "Reklam Hesap", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            wrapper.Controls.Add(save);

            contentPanel.Controls.Add(wrapper);
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
        }

        private void AddPriceHeader(TableLayoutPanel table, string a, string b, string c, string d)
        {
            AddPriceCell(table, a, 0, 0, true);
            AddPriceCell(table, b, 1, 0, true);
            AddPriceCell(table, c, 2, 0, true);
            AddPriceCell(table, d, 3, 0, true);
        }

        private void AddPriceRow(TableLayoutPanel table, string name, string option, string unit, decimal price, Action<decimal> saveAction)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            AddPriceCell(table, name, 0, row, false);
            AddPriceCell(table, option, 1, row, false);
            AddPriceCell(table, unit, 2, row, false);

            var n = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100000000,
                DecimalPlaces = 2,
                Increment = 1,
                Value = Math.Max(0, Math.Min(100000000, price)),
                Width = 150,
                Height = 30,
                Anchor = AnchorStyles.None,
                TextAlign = HorizontalAlignment.Right,
                Font = new Font("Segoe UI", 10F)
            };
            n.Tag = saveAction;
            n.Leave += (s, e) => saveAction(n.Value);
            table.Controls.Add(n, 3, row);
        }

        private void AddPriceCell(TableLayoutPanel table, string text, int column, int row, bool header)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = header ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", header ? 9F : 10F, header ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = header ? Color.DimGray : Color.FromArgb(35, 35, 35),
                Padding = new Padding(10, 0, 5, 0)
            };
            table.Controls.Add(label, column, row);
        }

        private void ShowProductInfo()
        {
            ShowSimplePage("Ürün Ekleme", "Ürün ekleme ekranı bir sonraki aşamada veritabanına bağlı olarak tamamlanacak.");
        }

        private void ShowKdv()
        {
            Controls.Remove(contentPanel);
            contentPanel?.Dispose();

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = backgroundColor,
                Padding = new Padding(25, 20, 25, 30)
            };

            var box = new Panel
            {
                Width = 620,
                Height = 240,
                BackColor = Color.White,
                Padding = new Padding(25)
            };

            var title = new Label
            {
                Text = "KDV AYARLARI",
                Font = new Font("Segoe UI Semibold", 18F),
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 45
            };
            box.Controls.Add(title);

            var info = new Label
            {
                Text = "Hesaplama sırasında KDV uygulanmasını seçin.",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 35
            };
            box.Controls.Add(info);

            var check = new CheckBox
            {
                Text = "%20 KDV Uygula",
                Checked = kdv20Aktif,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 12F),
                Dock = DockStyle.Top,
                Height = 42
            };
            check.CheckedChanged += (s, e) =>
            {
                kdv20Aktif = check.Checked;
                if (totalLabel != null)
                    CalculateTotal();
            };
            box.Controls.Add(check);

            var status = new Label
            {
                Text = kdv20Aktif ? "Durum: %20 KDV AKTİF" : "Durum: KDV uygulanmıyor",
                Font = new Font("Segoe UI", 10F),
                ForeColor = kdv20Aktif ? activeColor : Color.DimGray,
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 35,
                Padding = new Padding(0, 8, 0, 0)
            };
            check.CheckedChanged += (s, e) =>
            {
                status.Text = check.Checked ? "Durum: %20 KDV AKTİF" : "Durum: KDV uygulanmıyor";
                status.ForeColor = check.Checked ? activeColor : Color.DimGray;
            };
            box.Controls.Add(status);

            contentPanel.Controls.Add(box);
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
        }

        private void ShowProfit()
        {
            Controls.Remove(contentPanel);
            contentPanel?.Dispose();

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = backgroundColor,
                Padding = new Padding(25, 20, 25, 30)
            };

            var box = new Panel
            {
                Width = 620,
                Height = 290,
                BackColor = Color.White,
                Padding = new Padding(25)
            };

            var title = new Label
            {
                Text = "KAR / KAZANÇ AYARLARI",
                Font = new Font("Segoe UI Semibold", 18F),
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 45
            };
            box.Controls.Add(title);

            var info = new Label
            {
                Text = "Hesaplanan maliyetin üzerine uygulanacak kazanç oranını seçin.",
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 40
            };
            box.Controls.Add(info);

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 55,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var none = new RadioButton
            { Text = "Kazanç Yok", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 10, 25, 0),
              Checked = karYuzde == 0 };
            var fifty = new RadioButton
            { Text = "%50 Kazanç", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 10, 25, 0),
              Checked = karYuzde == 50 };
            var seventyFive = new RadioButton
            { Text = "%75 Kazanç", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 10, 25, 0),
              Checked = karYuzde == 75 };
            var hundred = new RadioButton
            { Text = "%100 Kazanç", AutoSize = true, Font = new Font("Segoe UI", 11F), Margin = new Padding(0, 10, 0, 0),
              Checked = karYuzde == 100 };

            EventHandler changed = (s, e) =>
            {
                if (none.Checked) karYuzde = 0;
                else if (fifty.Checked) karYuzde = 50;
                else if (seventyFive.Checked) karYuzde = 75;
                else if (hundred.Checked) karYuzde = 100;
                status.Text = karYuzde == 100 ? "Durum: %100 KAZANÇ AKTİF" :
                              karYuzde == 75 ? "Durum: %75 KAZANÇ AKTİF" :
                              karYuzde == 50 ? "Durum: %50 KAZANÇ AKTİF" :
                              "Durum: KAZANÇ UYGULANMIYOR";
                status.ForeColor = karYuzde > 0 ? activeColor : Color.DimGray;
                if (totalLabel != null) CalculateTotal();
            };
            none.CheckedChanged += changed;
            fifty.CheckedChanged += changed;
            seventyFive.CheckedChanged += changed;
            hundred.CheckedChanged += changed;

            panel.Controls.Add(none);
            panel.Controls.Add(fifty);
            panel.Controls.Add(seventyFive);
            panel.Controls.Add(hundred);
            box.Controls.Add(panel);

            var status = new Label
            {
                Text = karYuzde == 100 ? "Durum: %100 KAZANÇ AKTİF" :
                       karYuzde == 75 ? "Durum: %75 KAZANÇ AKTİF" :
                       karYuzde == 50 ? "Durum: %50 KAZANÇ AKTİF" :
                       "Durum: KAZANÇ UYGULANMIYOR",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = karYuzde > 0 ? activeColor : Color.DimGray,
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(0, 8, 0, 0)
            };
            box.Controls.Add(status);

            var example = new Label
            {
                Text = "Örnek: 1.000 TL maliyet → %50 = 1.500 TL | %75 = 1.750 TL | %100 = 2.000 TL",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Dock = DockStyle.Top,
                Height = 35
            };
            box.Controls.Add(example);

            contentPanel.Controls.Add(box);
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
        }

        private void ShowBackupInfo()
        {
            ShowSimplePage("Yedek Al / Geri Yükle", "Veritabanı yedekleme ve geri yükleme ekranı.");
        }

        private void ShowSimplePage(string title, string text)
        {
            Controls.Remove(contentPanel);
            contentPanel?.Dispose();
            contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = backgroundColor, Padding = new Padding(35) };
            var box = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Color.White, Padding = new Padding(25) };
            box.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI Semibold", 20F), AutoSize = true, Dock = DockStyle.Top });
            box.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 11F), AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 20, 0, 0) });
            contentPanel.Controls.Add(box);
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
        }
    }
}
