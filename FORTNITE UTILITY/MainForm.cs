using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FortniteUtility;

public class MainForm : Form
{
    private readonly bool _softCheck;
    private VersionSnapshot _versionSnapshot;

    private enum Language { En, Ru }
    private Language _lang = Language.En;

    private static readonly Dictionary<string, (string En, string Ru)> Strings = new()
    {
        ["subtitle"] = ("by arsenza - Fortnite settings helper", "by arsenza - помощник по настройке Fortnite"),
        ["clear_title"] = ("Clear Fortnite cache", "Очистить кэш Fortnite"),
        ["clear_desc"] = ("Clears shader and launcher caches, refreshes GameUserSettings.ini. Especially useful after major patches.", "Очищает кэш шейдеров и лаунчера, обновляет GameUserSettings.ini. Особенно полезно после крупных патчей."),
        ["status_backup"] = ("Backing up shaders...", "Бэкап шейдеров..."),
        ["status_restore"] = ("Restoring shaders...", "Восстанавливаем шейдеры..."),
        ["status_clean"] = ("Cleaning cache and shaders...", "Очищаем кэш и шейдеры..."),
        ["status_auto_setup"] = ("Auto setup...", "Автонастройка..."),
        ["status_download_config"] = ("Downloading config...", "Скачиваем конфиг..."),
        ["status_ready"] = ("Ready", "Готово"),
        ["clear_button"] = ("Clear cache", "Очистить кэш"),
        ["backup_button"] = ("Backup shaders", "Сделать бэкап шейдеров"),
        ["restore_button"] = ("Restore shaders", "Вернуть шейдеры"),
        ["install_title"] = ("Install GameUserSettings.ini", "Установить GameUserSettings.ini"),
        ["install_desc"] = ("Auto setup will reinstall the config, back up shaders, and clear the cache. Advanced settings let you tweak many more parameters.", "Автонастройка переустановит конфиг, сделает бэкап шейдеров и очистит кэш. Расширенные настройки позволяют менять гораздо больше параметров."),
        ["auto_setup"] = ("Auto setup", "Автонастройка"),
        ["advanced_settings"] = ("Advanced settings", "Расширенные настройки"),
        ["version_prefix"] = ("Version", "Версия"),
        ["auto_failed_title"] = ("Auto setup failed", "Автонастройка не удалась"),
        ["auto_failed_body"] = ("Could not download GameUserSettings.ini.", "Не удалось скачать GameUserSettings.ini."),
        ["auto_done_title"] = ("Auto setup complete", "Автонастройка завершена"),
        ["auto_done_ok"] = ("Config reinstalled, settings applied, shaders backed up, and cache cleared.", "Конфиг переустановлен, настройки применены, шейдеры сохранены, кэш очищен."),
        ["auto_done_no_backup"] = ("Config reinstalled and settings applied.\nCache cleanup skipped because shader backup could not be created.\nClose Fortnite/other games and try again. If it still fails, reboot and retry.", "Конфиг переустановлен, настройки применены.\nОчистка кэша пропущена, потому что не удалось сделать бэкап шейдеров.\nЗакройте Fortnite/другие игры и повторите попытку. Если не помогает - перезагрузите ПК и повторите попытку."),
        ["auto_done_no_shaders"] = ("Config reinstalled and settings applied.\nCache cleanup skipped because shader/cache folders were not found.\nStart Fortnite once, then try again.", "Конфиг переустановлен, настройки применены.\nОчистка кэша пропущена, потому что папки кэша шейдеров не найдены.\nЗапустите Fortnite хотя бы один раз и повторите попытку."),
        ["auto_done_cache_failed"] = ("Config reinstalled and settings applied.\nShader backup created, but cache cleanup failed.\nClose Fortnite/other games and try again. If it still fails, reboot and retry.", "Конфиг переустановлен, настройки применены.\nБэкап шейдеров создан, но очистка кэша не удалась.\nЗакройте Fortnite/другие игры и повторите попытку. Если не помогает - перезагрузите ПК и повторите попытку."),
        ["done_title"] = ("Done", "Готово"),
        ["clear_done_body"] = ("Shader cleanup is complete!\nRestart your computer.", "Очистка шейдеров завершена!\nПерезагрузите компьютер."),
        ["backup_success_title"] = ("Backup created", "Бэкап создан"),
        ["backup_success_body"] = ("Shader/cache backup saved next to the app.", "Бэкап шейдеров/кэша сохранен рядом с программой."),
        ["restore_success_title"] = ("Restore complete", "Восстановление завершено"),
        ["restore_success_body"] = ("Shader/cache folders restored from backup.", "Папки шейдеров/кэша восстановлены из бэкапа."),
        ["graphics_title"] = ("GameUserSettings & DirectX presets", "GameUserSettings и пресеты DirectX"),
        ["graphics_desc"] = ("Install the latest GameUserSettings.ini, then choose a rendering profile:", "Установите свежий GameUserSettings.ini, затем выберите профиль рендеринга:"),
        ["reinstall_button"] = ("Reinstall GameUserSettings.ini", "Переустановить GameUserSettings.ini"),
        ["install_button"] = ("Install GameUserSettings.ini", "Установить GameUserSettings.ini"),
        ["confirm_replace_title"] = ("Confirm", "Подтвердите"),
        ["confirm_replace_body"] = ("GameUserSettings.ini already exists. Replace it with the updated version?", "GameUserSettings.ini уже существует. Заменить на обновленный?"),
        ["confirm_replace_yes"] = ("Replace", "Заменить"),
        ["cancel"] = ("Cancel", "Отмена"),
        ["install_error_title"] = ("Error", "Ошибка"),
        ["install_error_body"] = ("Failed to download GameUserSettings.ini. Check your internet connection.", "Не удалось скачать GameUserSettings.ini, проверьте подключение к интернету."),
        ["install_done_title"] = ("Installed", "Установлено"),
        ["install_done_body"] = ("GameUserSettings.ini has been installed.", "GameUserSettings.ini установлен."),
        ["dx11_perf"] = ("DX11 Performance (recommended)", "DX11 Performance (рекомендуется)"),
        ["dx11_default"] = ("DX11 Default", "DX11 Default"),
        ["dx12_perf"] = ("DX12 Performance (experimental)", "DX12 Performance (experimental)"),
        ["dx12_default"] = ("DX12 Default", "DX12 Default"),
        ["dx11_perf_adv"] = ("DX11 Performance\r\n(recommended)", "DX11 Performance\r\n(рекомендуется)"),
        ["dx12_perf_adv"] = ("DX12 Performance\r\n(experimental)", "DX12 Performance\r\n(experimental)"),
        ["close_button"] = ("Close", "Закрыть"),
        ["backup_prompt_title"] = ("Back up shaders?", "Сделать бэкап шейдеров?"),
        ["backup_prompt_body"] = ("No shader/cache backup found. Create one before clearing?", "Бэкап шейдеров/кэша не найден. Создать перед очисткой?"),
        ["restore_prompt_title"] = ("Restore shaders?", "Вернуть шейдеры?"),
        ["restore_prompt_body"] = ("A shader/cache backup was found. Restore it before clearing?", "Найден бэкап шейдеров/кэша. Восстановить перед очисткой?"),
        ["clear_confirm_title"] = ("Clear Fortnite cache", "Очистить кэш Fortnite"),
        ["clear_confirm_body"] = ("Clearing will remove shaders and launcher data. The first launches may stutter until shaders rebuild. Continue?", "Очистка удалит шейдеры и данные лаунчера. Первые запуски могут подлагивать, пока шейдеры пересоберутся. Продолжить?"),
        ["backup_failed_title"] = ("Backup failed", "Бэкап не удался"),
        ["backup_failed_body"] = ("Could not create a shader backup. Close Fortnite/other games and try again. If it still fails, reboot and retry.", "Не удалось сделать бэкап шейдеров. Закройте Fortnite/другие игры и повторите попытку. Если не помогает - перезагрузите ПК и повторите попытку."),
        ["backup_missing_title"] = ("Shader folders not found", "Папки шейдеров не найдены"),
        ["backup_missing_body"] = ("Shader/cache folders were not found, so a backup can't be created.\nStart Fortnite once, then try again.", "Папки кэша шейдеров не найдены, поэтому бэкап создать не удалось.\nЗапустите Fortnite хотя бы один раз и повторите попытку."),
        ["restore_failed_title"] = ("Restore failed", "Восстановление не удалось"),
        ["restore_failed_body"] = ("Could not restore shader backup. Make sure a backup exists and files aren't being used by another program.", "Не удалось вернуть бекап шейдеров. Проверьте, что бэкап есть или они не заняты другой программой."),
        ["details_label"] = ("Details:", "Детали:"),
        ["clear_failed_title"] = ("Cleanup failed", "Очистка не удалась"),
        ["clear_failed_body"] = ("Some cache files could not be removed. Close Fortnite/Epic and try again.", "Не удалось удалить некоторые файлы кэша. Закройте Fortnite/Epic и повторите попытку."),
        ["download_missing_title"] = ("Config missing", "Конфиг не найден"),
        ["download_missing_body"] = ("GameUserSettings.ini not found. Download it now?", "GameUserSettings.ini не найден. Скачать сейчас?"),
        ["download_failed_title"] = ("Download failed", "Скачивание не удалось"),
        ["download_failed_body"] = ("Could not download GameUserSettings.ini.", "Не удалось скачать GameUserSettings.ini."),
        ["advanced_title"] = ("Advanced settings", "Расширенные настройки"),
        ["reflex_label"] = ("NVIDIA Reflex:", "NVIDIA Reflex:"),
        ["resolution_label"] = ("Resolution (width x height):", "Разрешение (ширина x высота):"),
        ["display_mode_label"] = ("Display mode:", "Режим отображения:"),
        ["fps_label"] = ("Frame rate limit:", "Лимит FPS:"),
        ["resolution_scale_label"] = ("Resolution scale (sg.ResolutionQuality):", "Масштаб разрешения (sg.ResolutionQuality):"),
        ["render_api_label"] = ("Render mode:", "Режим рендера:"),
        ["save_button"] = ("Save", "Сохранить"),
        ["cancel_button"] = ("Cancel", "Отмена"),
        ["reflex_off"] = ("Off", "Выкл"),
        ["reflex_on"] = ("On", "Вкл"),
        ["reflex_boost"] = ("On + Boost", "Вкл + Буст"),
        ["fullscreen"] = ("Fullscreen", "Полноэкранный"),
        ["windowed_full"] = ("Windowed Fullscreen", "Окно на весь экран"),
        ["windowed"] = ("Windowed", "Оконный"),
        ["fps_60"] = ("60", "60"),
        ["fps_120"] = ("120", "120"),
        ["fps_144"] = ("144", "144"),
        ["fps_165"] = ("165", "165"),
        ["fps_180"] = ("180", "180"),
        ["fps_240"] = ("240", "240"),
        ["fps_360"] = ("360", "360"),
        ["fps_unlimited"] = ("Unlimited", "Без ограничений"),
        ["fps_custom"] = ("Custom", "Свое"),
        ["advanced_saved_title"] = ("Saved", "Сохранено"),
        ["advanced_saved_body"] = ("Advanced settings applied.", "Расширенные настройки применены."),
        ["advanced_save_failed"] = ("Save failed", "Не удалось сохранить"),
        ["advanced_save_failed_body"] = ("Could not apply advanced settings. Ensure GameUserSettings.ini is writable.", "Не удалось применить расширенные настройки. Проверьте доступ к GameUserSettings.ini."),
        ["modify_error_title"] = ("Error", "Ошибка"),
        ["modify_error_body"] = ("Could not modify GameUserSettings.ini.", "Не удалось изменить GameUserSettings.ini."),
        ["preset_dx11_perf_done"] = ("Applied: DirectX11 Performance (recommended).", "Применено: DirectX11 Performance (рекомендуется)."),
        ["preset_dx11_default_done"] = ("Applied: Default DirectX11.", "Применено: DirectX11 Default."),
        ["preset_dx12_perf_done"] = ("Applied: DirectX12 Performance (experimental).", "Применено: DirectX12 Performance (experimental)."),
        ["preset_dx12_default_done"] = ("Applied: DirectX12 Default.", "Применено: DirectX12 Default."),
        ["update_available_title"] = ("Update available", "Доступно обновление"),
        ["update_available_body"] = ("Found a new version: {0}\nOpen the releases page?", "Найдена новая версия: {0}\nОткрыть страницу релизов?"),
        ["update_open"] = ("Open", "Открыть"),
        ["update_later"] = ("Later", "Позже"),
        ["skip_version_title"] = ("Skip this version?", "Пропустить эту версию?"),
        ["skip_version_body"] = ("Don't remind about {0}?", "Не напоминать о {0}?"),
        ["skip_yes"] = ("Skip", "Пропустить"),
        ["skip_no"] = ("Keep", "Оставить")
    };

    private Label _versionLink = null!;
    private FlowLayoutPanel _stackPanel = null!;
    private Panel _bodyPanel = null!;

    private Label _subtitleLabel = null!;
    private Label _promoLink = null!;
    private Button _langButton = null!;

    private Label _clearTitleLabel = null!;
    private Label _clearDescLabel = null!;

    private Label _installTitleLabel = null!;
    private Label _installDescLabel = null!;

    private Button _clearButton = null!;
    private Button _graphicsButton = null!;
    private Button _autoSetupButton = null!;
    private Button _advancedButton = null!;
    private Button _backupButton = null!;
    private Button _restoreButton = null!;

    private string Tr(string key)
    {
        if (Strings.TryGetValue(key, out var pair))
        {
            return _lang == Language.Ru ? pair.Ru : pair.En;
        }

        return key;
    }

    private string WithDetails(string message)
    {
        var details = UtilityActions.ConsumeLastError();
        if (string.IsNullOrWhiteSpace(details))
        {
            return message;
        }

        return $"{message}\n\n{Tr("details_label")}\n{details}";
    }

    public MainForm(bool softCheck)
    {
        _softCheck = softCheck;
        _versionSnapshot = VersionManager.EnsureSnapshot();

        InitializeComponent();
        BuildCards();
        UpdateVersionLabel(_versionSnapshot.Version);
        ApplyLanguage();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        BeginInvoke(async () => await CheckForUpdatesAsync());
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Theme.BodyFont;
        BackColor = Theme.Background;
        ForeColor = Theme.TextPrimary;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(920, 640);
        Text = "FORTNITE UTILITY";

        var header = BuildHeader();
        _bodyPanel = BuildBody();
        var footer = BuildFooter();

        Controls.Add(_bodyPanel);
        Controls.Add(footer);
        Controls.Add(header);
        ResumeLayout();
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 140,
            Padding = new Padding(22, 18, 22, 12),
            BackColor = Theme.Surface
        };

        var headerContent = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            Padding = new Padding(0),
            Margin = new Padding(0),
            Anchor = AnchorStyles.None
        };
        headerContent.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = new Label
        {
            AutoSize = true,
            Text = "FORTNITE UTILITY",
            Font = Theme.HeadingFont,
            ForeColor = Theme.TextPrimary,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 6)
        };

        _subtitleLabel = new Label
        {
            AutoSize = true,
            Text = Tr("subtitle"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextMuted,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 6)
        };

        _promoLink = new Label
        {
            AutoSize = true,
            Text = "https://t.me/arsenzaa",
            Font = Theme.Small,
            ForeColor = Theme.AccentSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.None,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        var url = "https://t.me/arsenzaa";
        _promoLink.Click += (_, _) => TryOpenUrl(url);

        headerContent.Controls.Add(title, 0, 0);
        headerContent.Controls.Add(_subtitleLabel, 0, 1);
        headerContent.Controls.Add(_promoLink, 0, 2);

        header.Controls.Add(headerContent);
        header.Resize += (_, _) => CenterContent(header, headerContent);
        headerContent.SizeChanged += (_, _) => CenterContent(header, headerContent);
        CenterContent(header, headerContent);
        _langButton = CreateAccentButton("EN / RU", Theme.SurfaceAlt);
        _langButton.MinimumSize = Size.Empty;
        _langButton.AutoSize = true;
        _langButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _langButton.Padding = new Padding(8, 6, 8, 6);
        _langButton.Font = Theme.Small;
        _langButton.Click += (_, _) => ToggleLanguage();
        header.Controls.Add(_langButton);
        _langButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _langButton.Location = new Point(header.Width - _langButton.Width - 12, 12);
        header.Resize += (_, _) =>
        {
            _langButton.Left = header.ClientSize.Width - _langButton.Width - 12;
        };
        return header;
    }

    private Panel BuildBody()
    {
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            AutoScroll = true,
            Padding = new Padding(0, 12, 0, 12)
        };

        _stackPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Background,
            Padding = new Padding(0),
            Margin = new Padding(0),
            Location = new Point(0, 0)
        };

        body.Controls.Add(_stackPanel);
        body.Resize += (_, _) => CenterStack(body);
        CenterStack(body);
        return body;
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(22, 8, 22, 8),
            BackColor = Theme.Surface
        };

        var footerContent = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        _versionLink = new Label
        {
            AutoSize = true,
            Text = $"{Tr("version_prefix")}: --",
            Font = Theme.Small,
            ForeColor = Theme.AccentSecondary,
            Anchor = AnchorStyles.None,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        _versionLink.Click += (_, _) => TryOpenUrl("https://github.com/arsenzaaa/FORTNITE-UTILITY");

        footerContent.Controls.Add(_versionLink);

        footer.Controls.Add(footerContent);
        footer.Resize += (_, _) => CenterContent(footer, footerContent);
        CenterContent(footer, footerContent);
        return footer;
    }

    private void BuildCards()
    {
        _stackPanel.SuspendLayout();
        _stackPanel.Controls.Clear();

        _stackPanel.Controls.Add(CreateCard(
            Tr("clear_title"),
            Tr("clear_desc"),
            Tr("clear_button"),
            Theme.SurfaceAlt,
            out _clearButton,
            ClearCacheClicked,
            out _clearTitleLabel,
            out _clearDescLabel,
            (Tr("backup_button"), Theme.SurfaceAlt, BackupShadersClicked),
            (Tr("restore_button"), Theme.SurfaceAlt, RestoreShadersClicked)));
        var clearButtons = (_stackPanel.Controls[0] as Panel)?.Tag as List<Button>;
        if (clearButtons != null && clearButtons.Count >= 3)
        {
            _backupButton = clearButtons[1];
            _restoreButton = clearButtons[2];
        }

        SetClearButtonState(cleaned: false);

        _stackPanel.Controls.Add(CreateCard(
            Tr("install_title"),
            Tr("install_desc"),
            Tr("auto_setup"),
            Theme.Accent,
            out _graphicsButton,
            AutoSetupClicked,
            out _installTitleLabel,
            out _installDescLabel,
            (Tr("advanced_settings"), Theme.SurfaceAlt, AdvancedSettingsClicked)));

        var buttons = (_stackPanel.Controls[1] as Panel)?.Tag as List<Button>;
        if (buttons != null && buttons.Count >= 2)
        {
            _autoSetupButton = buttons[0];
            _advancedButton = buttons[1];
        }

        _stackPanel.ResumeLayout();
        CenterStack(_bodyPanel);
    }

    private static void CenterContent(Control container, Control content)
    {
        var bounds = container.DisplayRectangle;
        content.Left = bounds.Left + Math.Max(0, (bounds.Width - content.Width) / 2);
        content.Top = bounds.Top + Math.Max(0, (bounds.Height - content.Height) / 2);
    }

    private void CenterStack(Control host)
    {
        if (_stackPanel == null) return;
        int targetWidth = Math.Min(780, Math.Max(520, host.ClientSize.Width - 60));
        _stackPanel.MaximumSize = new Size(targetWidth, 0);
        _stackPanel.MinimumSize = new Size(targetWidth, 0);
        _stackPanel.Left = Math.Max(22, (host.ClientSize.Width - targetWidth) / 2);
        _stackPanel.PerformLayout();
        int availableHeight = host.ClientSize.Height;
        int centeredTop = Math.Max(10, (availableHeight - _stackPanel.Height) / 2);
        _stackPanel.Top = centeredTop;

        foreach (Control c in _stackPanel.Controls)
        {
            if (c is Panel p)
            {
                p.Width = targetWidth;
            }
        }
    }

    private Panel CreateCard(
        string title,
        string description,
        string buttonText,
        Color accent,
        out Button actionButton,
        EventHandler onClick,
        out Label titleLabel,
        out Label descLabel,
        params (string text, Color accent, EventHandler handler)[] extraActions)
    {
        var card = new Panel
        {
            BackColor = Theme.Background,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(18, 14, 18, 14),
            Width = 760,
            Height = 150
        };

        card.Paint += (_, e) =>
        {
            using var brush = new SolidBrush(card.BackColor);
            e.Graphics.FillRectangle(brush, card.ClientRectangle);
            using var pen = new Pen(Theme.AccentSecondary, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var accentLine = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Theme.Accent
        };

        titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Font = Theme.BodyBold,
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            Location = new Point(8, 14)
        };

        descLabel = new Label
        {
            AutoSize = false,
            Text = description,
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            BackColor = Color.Transparent,
            Location = new Point(8, 44),
            Size = new Size(520, 0)
        };

        actionButton = CreateAccentButton(buttonText, accent);
        actionButton.Click += onClick;
        actionButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var buttons = new List<Button> { actionButton };
        foreach (var extra in extraActions)
        {
            var btn = CreateAccentButton(extra.text, extra.accent);
            btn.Click += extra.handler;
            btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttons.Add(btn);
        }

        foreach (var btn in buttons)
        {
            card.Controls.Add(btn);
        }
        card.Controls.Add(descLabel);
        card.Controls.Add(titleLabel);
        card.Controls.Add(accentLine);

        var buttonsLocal = buttons.ToList();
        var descLocal = descLabel;

        void LayoutCard()
        {
            const int spacing = 10;
            const int descBottomPadding = 16;
            const int bottomPadding = 14;
            const int minDescriptionWidth = 200;
            const int minCardHeight = 150;
            const int rightPadding = 8;

            int totalWidth = buttonsLocal.Sum(b => b.Width) + spacing * (buttonsLocal.Count - 1);
            int x = card.ClientSize.Width - totalWidth - 12;
            int descWidth = Math.Max(minDescriptionWidth, card.ClientSize.Width - descLocal.Left - rightPadding);
            if (descLocal.Width != descWidth)
            {
                descLocal.Width = descWidth;
            }

            int measuredHeight = TextRenderer.MeasureText(
                descLocal.Text ?? string.Empty,
                descLocal.Font,
                new Size(descWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;

            if (descLocal.Height != measuredHeight)
            {
                descLocal.Height = measuredHeight;
            }

            int requiredHeight = Math.Max(
                minCardHeight,
                descLocal.Bottom + descBottomPadding + buttonsLocal[0].Height + bottomPadding);

            if (card.Height != requiredHeight)
            {
                card.Height = requiredHeight;
            }

            int y = card.ClientSize.Height - buttonsLocal[0].Height - bottomPadding;

            foreach (var btn in buttonsLocal)
            {
                btn.Location = new Point(x, y);
                x += btn.Width + spacing;
            }
        }

        LayoutCard();
        card.Resize += (_, _) => LayoutCard();
        descLocal.TextChanged += (_, _) => LayoutCard();
        foreach (var btn in buttonsLocal)
        {
            btn.SizeChanged += (_, _) => LayoutCard();
        }
        card.Tag = buttons;

        return card;
    }

    private Button CreateAccentButton(string text, Color accent)
    {
        bool filled = accent == Theme.Accent || accent == Theme.Danger;
        var baseBack = filled ? Theme.Accent : Theme.Background;
        var borderColor = filled ? Theme.Background : Theme.Accent;
        var textColor = filled ? Theme.Background : Theme.TextPrimary;

        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(120, 38),
            FlatStyle = FlatStyle.Flat,
            Font = Theme.BodyBold,
            BackColor = baseBack,
            ForeColor = textColor,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(12, 10, 12, 10),
            UseCompatibleTextRendering = true
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = borderColor;
        btn.FlatAppearance.MouseOverBackColor = baseBack;
        btn.FlatAppearance.MouseDownBackColor = baseBack;
        return btn;
    }

    private void SetClearButtonState(bool cleaned)
    {
        if (_clearButton == null) return;

        _clearButton.BackColor = Theme.Background;
        _clearButton.ForeColor = Theme.TextPrimary;
        _clearButton.FlatAppearance.BorderColor = Theme.Accent;
        _clearButton.FlatAppearance.MouseOverBackColor = _clearButton.BackColor;
        _clearButton.FlatAppearance.MouseDownBackColor = _clearButton.BackColor;
    }

    private async void ClearCacheClicked(object? sender, EventArgs e)
    {
        var hasBackup = UtilityActions.HasShaderBackup();

        if (!hasBackup)
        {
            var makeBackup = Dialogs.Confirm(this, Tr("backup_prompt_title"),
                Tr("backup_prompt_body"),
                Tr("confirm_replace_yes"), Tr("cancel"));
            if (makeBackup != DialogResult.Yes)
            {
                return;
            }

            UtilityActions.ShaderBackupResult backupResult = UtilityActions.ShaderBackupResult.Failed;
            await RunWithBusyState(async () =>
            {
                backupResult = await UtilityActions.BackupShadersAsync();
            }, Tr("status_backup"));

            if (backupResult != UtilityActions.ShaderBackupResult.Success)
            {
                var titleKey = backupResult == UtilityActions.ShaderBackupResult.NotFound
                    ? "backup_missing_title"
                    : "backup_failed_title";
                var bodyKey = backupResult == UtilityActions.ShaderBackupResult.NotFound
                    ? "backup_missing_body"
                    : "backup_failed_body";
                var body = Tr(bodyKey);
                if (backupResult == UtilityActions.ShaderBackupResult.Failed)
                {
                    body = WithDetails(body);
                }
                Dialogs.Error(this, Tr(titleKey), body);
                return;
            }
            hasBackup = true;
        }
        else
        {
            var restore = Dialogs.Confirm(this, Tr("restore_prompt_title"),
                Tr("restore_prompt_body"),
                Tr("confirm_replace_yes"), Tr("cancel"));
            if (restore == DialogResult.Yes)
            {
                var ok = await RunWithResultAsync(Tr("status_restore"), UtilityActions.RestoreShadersAsync);
                if (!ok)
                {
                    Dialogs.Error(this, Tr("restore_failed_title"), WithDetails(Tr("restore_failed_body")));
                    return;
                }
            }
        }

        var warn = Tr("clear_confirm_body");
        if (Dialogs.Confirm(this, Tr("clear_confirm_title"), warn, Tr("confirm_replace_yes"), Tr("cancel")) != DialogResult.Yes)
        {
            return;
        }

        bool cleared = false;
        await RunWithBusyState(async () =>
        {
            cleared = await UtilityActions.ClearCacheAsync();
        }, Tr("status_clean"));

        if (!cleared)
        {
            Dialogs.Error(this, Tr("clear_failed_title"), WithDetails(Tr("clear_failed_body")));
            return;
        }

        SetClearButtonState(cleaned: true);
        Dialogs.Info(this, Tr("done_title"), Tr("clear_done_body"));
    }

    private async void BackupShadersClicked(object? sender, EventArgs e)
    {
        await RunWithBusyState(async () =>
        {
            var backupResult = await UtilityActions.BackupShadersAsync();
            if (backupResult != UtilityActions.ShaderBackupResult.Success)
            {
                var titleKey = backupResult == UtilityActions.ShaderBackupResult.NotFound
                    ? "backup_missing_title"
                    : "backup_failed_title";
                var bodyKey = backupResult == UtilityActions.ShaderBackupResult.NotFound
                    ? "backup_missing_body"
                    : "backup_failed_body";
                var body = Tr(bodyKey);
                if (backupResult == UtilityActions.ShaderBackupResult.Failed)
                {
                    body = WithDetails(body);
                }
                Dialogs.Error(this, Tr(titleKey), body);
                return;
            }

            Dialogs.Info(this, Tr("backup_success_title"), Tr("backup_success_body"));
        }, Tr("status_backup"));
    }

    private async void RestoreShadersClicked(object? sender, EventArgs e)
    {
        await RunWithBusyState(async () =>
        {
            var ok = await UtilityActions.RestoreShadersAsync();
            if (!ok)
            {
                Dialogs.Error(this, Tr("restore_failed_title"), WithDetails(Tr("restore_failed_body")));
                return;
            }

            Dialogs.Info(this, Tr("restore_success_title"), Tr("restore_success_body"));
        }, Tr("status_restore"));
    }

    private async void AutoSetupClicked(object? sender, EventArgs e)
    {
        bool downloadOk = false;
        UtilityActions.ShaderBackupResult backupResult = UtilityActions.ShaderBackupResult.Failed;
        bool cacheCleared = false;
        await RunWithBusyState(async () =>
        {
            downloadOk = await UtilityActions.DownloadGameUserSettingsAsync();
            if (!downloadOk) return;

            int hz = UtilityActions.GetPrimaryMonitorRefreshRateHz();
            if (hz > 0)
            {
                _ = await UtilityActions.ApplyFrameRateLimitAsync(hz);
            }

            var (monW, monH) = UtilityActions.GetPrimaryMonitorResolution();
            if (monW > 0 && monH > 0)
            {
                _ = await UtilityActions.ApplyResolutionAsync(monW, monH);
            }

            backupResult = await UtilityActions.BackupShadersAsync();
            if (backupResult != UtilityActions.ShaderBackupResult.Success) return;
            cacheCleared = await UtilityActions.ClearCacheAsync();
        }, Tr("status_auto_setup"));

        if (!downloadOk)
        {
            Dialogs.Error(this, Tr("auto_failed_title"), WithDetails(Tr("auto_failed_body")));
            return;
        }

        var doneBodyKey = backupResult switch
        {
            UtilityActions.ShaderBackupResult.Success => cacheCleared ? "auto_done_ok" : "auto_done_cache_failed",
            UtilityActions.ShaderBackupResult.NotFound => "auto_done_no_shaders",
            _ => "auto_done_no_backup"
        };

        var doneBody = Tr(doneBodyKey);
        if (backupResult == UtilityActions.ShaderBackupResult.Failed || (backupResult == UtilityActions.ShaderBackupResult.Success && !cacheCleared))
        {
            doneBody = WithDetails(doneBody);
        }
        Dialogs.Info(this, Tr("auto_done_title"), doneBody);

        // Auto setup also clears caches; update button style accordingly.
        if (cacheCleared)
        {
            SetClearButtonState(cleaned: true);
        }
    }

    private async void AdvancedSettingsClicked(object? sender, EventArgs e)
    {
        await ShowAdvancedSettingsDialogAsync();
    }

    private void ShowGraphicsDialog()
    {
        using var dlg = new Form
        {
            Text = Tr("graphics_title"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            AutoScaleMode = AutoScaleMode.Dpi,
            Font = Theme.BodyFont,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            ClientSize = new Size(640, 320)
        };

        dlg.SuspendLayout();

        var desc = new Label
        {
            AutoSize = true,
            Text = Tr("graphics_desc"),
            Font = Theme.Small,
            ForeColor = Theme.TextMuted,
            Location = new Point(14, 12),
            TextAlign = ContentAlignment.MiddleCenter
        };
        dlg.Controls.Add(desc);

        var hasConfig = File.Exists(UtilityActions.GameUserSettingsPath);

        Action<bool> enablePresetButtons = _ => { };

        var installButton = CreateAccentButton(
            hasConfig ? Tr("reinstall_button") : Tr("install_button"),
            Theme.Accent);
        installButton.Height = 40;
        installButton.Click += async (_, _) =>
        {
            if (File.Exists(UtilityActions.GameUserSettingsPath))
            {
                var overwrite = Dialogs.Confirm(dlg, Tr("confirm_replace_title"),
                    Tr("confirm_replace_body"),
                    Tr("confirm_replace_yes"), Tr("cancel"));
                if (overwrite != DialogResult.Yes)
                {
                    return;
                }
            }

            await RunDialogActionAsync(dlg, async () =>
            {
                var ok = await UtilityActions.DownloadGameUserSettingsAsync();
                if (!ok)
                {
                    Dialogs.Error(dlg, Tr("install_error_title"), WithDetails(Tr("install_error_body")));
                    return;
                }

                 Dialogs.Info(dlg, Tr("install_done_title"), Tr("install_done_body"));
                 installButton.Text = Tr("reinstall_button");
                 enablePresetButtons(true);
             });
         };
        dlg.Controls.Add(installButton);

        var grid = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            Location = new Point(14, 86),
            Width = dlg.ClientSize.Width - 28,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var dx11Perf = CreateAccentButton(Tr("dx11_perf"), Theme.SurfaceAlt);
        dx11Perf.Margin = new Padding(6, 6, 6, 6);
        dx11Perf.Click += async (_, _) => await ApplyPresetAsync(dlg, "dx11", "es31", false, Tr("preset_dx11_perf_done"));

        var dx11Default = CreateAccentButton(Tr("dx11_default"), Theme.SurfaceAlt);
        dx11Default.Margin = new Padding(6, 6, 6, 6);
        dx11Default.Click += async (_, _) => await ApplyPresetAsync(dlg, "dx11", "sm5", false, Tr("preset_dx11_default_done"));

        var dx12Perf = CreateAccentButton(Tr("dx12_perf"), Theme.SurfaceAlt);
        dx12Perf.Margin = new Padding(6, 6, 6, 6);
        dx12Perf.Click += async (_, _) => await ApplyPresetAsync(dlg, "dx12", "es31", true, Tr("preset_dx12_perf_done"));

        var dx12Default = CreateAccentButton(Tr("dx12_default"), Theme.SurfaceAlt);
        dx12Default.Margin = new Padding(6, 6, 6, 6);
        dx12Default.Click += async (_, _) => await ApplyPresetAsync(dlg, "dx12", "sm6", true, Tr("preset_dx12_default_done"));

        var presetButtons = new[] { dx11Perf, dx11Default, dx12Perf, dx12Default };
        enablePresetButtons = enabled =>
        {
            foreach (var b in presetButtons)
            {
                b.Enabled = enabled;
            }
        };
        enablePresetButtons(hasConfig);

        grid.Controls.Add(dx11Perf, 0, 0);
        grid.Controls.Add(dx11Default, 1, 0);
        grid.Controls.Add(dx12Perf, 0, 1);
        grid.Controls.Add(dx12Default, 1, 1);

        dlg.Controls.Add(grid);

        var close = CreateAccentButton(Tr("close_button"), Theme.SurfaceAlt);
        close.ForeColor = Theme.TextPrimary;
        close.Width = 120;
        close.Height = 34;
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        close.Click += (_, _) => dlg.Close();
        dlg.CancelButton = close;
        dlg.Controls.Add(close);

        void LayoutPresetDialog()
        {
            installButton.Width = dlg.ClientSize.Width - 28;
            grid.Width = installButton.Width;

            desc.MaximumSize = new Size(dlg.ClientSize.Width - 28, 0);

            int btnWidth = Math.Max(140, (grid.Width / 2) - 12);
            dx11Perf.Width = btnWidth;
            dx11Default.Width = btnWidth;
            dx12Perf.Width = btnWidth;
            dx12Default.Width = btnWidth;

            int spacingDesc = 8;
            int spacingInstallGrid = 12;
            int spacingGridClose = 14;

            int blockHeight = desc.Height + spacingDesc + installButton.Height + spacingInstallGrid + grid.Height + spacingGridClose + close.Height;
            int top = Math.Max(12, (dlg.ClientSize.Height - blockHeight) / 2);

            desc.Left = (dlg.ClientSize.Width - desc.Width) / 2;
            desc.Top = top;

            installButton.Left = (dlg.ClientSize.Width - installButton.Width) / 2;
            installButton.Top = desc.Bottom + spacingDesc;

            grid.Left = (dlg.ClientSize.Width - grid.Width) / 2;
            grid.Top = installButton.Bottom + spacingInstallGrid;

        close.Left = (dlg.ClientSize.Width - close.Width) / 2;
        close.Top = grid.Bottom + spacingGridClose;
    }

        LayoutPresetDialog();
        dlg.Resize += (_, _) => LayoutPresetDialog();

        dlg.ResumeLayout(false);
        dlg.PerformLayout();
        dlg.ShowDialog(this);
    }

    private async Task ShowAdvancedSettingsDialogAsync()
    {
        if (!File.Exists(UtilityActions.GameUserSettingsPath))
        {
            var confirm = Dialogs.Confirm(this, Tr("download_missing_title"),
                Tr("download_missing_body"),
                Tr("install_button"), Tr("cancel"));
            if (confirm != DialogResult.Yes) return;

            var ok = await RunWithResultAsync(Tr("status_download_config"), UtilityActions.DownloadGameUserSettingsAsync);
            if (!ok)
            {
                Dialogs.Error(this, Tr("download_failed_title"), WithDetails(Tr("download_failed_body")));
                return;
            }
        }

        var snapshot = UtilityActions.GetSettingsSnapshot();

        using var dlg = new Form
        {
            Text = Tr("advanced_title"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            AutoScaleMode = AutoScaleMode.Dpi,
            Font = Theme.BodyFont,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            ClientSize = new Size(660, 520),
            MinimumSize = new Size(660, 520),
            AutoScroll = false
        };

        dlg.SuspendLayout();

        ComboBox MakeCombo((string text, int value)[] options, int selected)
        {
            var cb = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220,
                BackColor = Theme.SurfaceAlt,
                ForeColor = Theme.TextPrimary,
                FormattingEnabled = true
            };
            foreach (var opt in options)
            {
                cb.Items.Add(opt);
                if (opt.value == selected)
                {
                    cb.SelectedItem = opt;
                }
            }
            if (cb.SelectedIndex < 0 && cb.Items.Count > 0) cb.SelectedIndex = 0;
            cb.Format += (_, e) =>
            {
                if (e.ListItem is ValueTuple<string, int> o) e.Value = o.Item1;
            };
            return cb;
        }

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10, 6, 10, 14)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        bool hasConfig = File.Exists(UtilityActions.GameUserSettingsPath);
        var reinstallButton = CreateAccentButton(hasConfig ? Tr("reinstall_button") : Tr("install_button"), Theme.SurfaceAlt);
        reinstallButton.MinimumSize = new Size(180, 32);
        reinstallButton.Anchor = AnchorStyles.Left;
        reinstallButton.Margin = new Padding(0, 0, 0, 0);
        var headerHost = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 6, 0, 8),
            BackColor = Theme.Surface
        };
        headerHost.Controls.Add(reinstallButton);
        headerHost.Resize += (_, _) => CenterContent(headerHost, reinstallButton);
        CenterContent(headerHost, reinstallButton);

        var grid = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = new Padding(0),
            Padding = new Padding(6, 10, 6, 6),
            BackColor = Theme.Surface
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var gridHost = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Padding = new Padding(0),
            BackColor = Theme.Surface
        };
        gridHost.Controls.Add(grid);

        void LayoutGrid()
        {
            int targetWidth = Math.Min(820, Math.Max(640, dlg.ClientSize.Width - 32));
            grid.MinimumSize = new Size(580, 0);
            grid.MaximumSize = new Size(targetWidth, 0);
            grid.Width = targetWidth;
            CenterContent(gridHost, grid);
        }

        gridHost.Resize += (_, _) => LayoutGrid();
        dlg.Resize += (_, _) => LayoutGrid();
        LayoutGrid();

        void AddRow(string labelText, Control control, int bottomMargin = 14, bool addSpacer = true)
        {
            int row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var inner = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Padding = new Padding(4, 4, 4, 2),
                BackColor = Theme.Surface
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lbl = new Label
            {
                AutoSize = true,
                Text = labelText,
                Font = Theme.BodyBold,
                ForeColor = Theme.TextPrimary,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, 4)
            };
            control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            var cm = control.Margin;
            int compactBottom = Math.Min(bottomMargin, 10);
            control.Margin = new Padding(cm.Left, Math.Max(cm.Top, 2), cm.Right, Math.Max(cm.Bottom, compactBottom));

            inner.Controls.Add(lbl, 0, 0);
            inner.Controls.Add(control, 0, 1);

            var rowPanel = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 8, 10, 10),
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Theme.Surface,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Dock = DockStyle.Fill,
                MinimumSize = new Size(600, 0)
            };
            rowPanel.Controls.Add(inner);
            inner.Dock = DockStyle.Top;
            inner.MinimumSize = new Size(520, 0);
            inner.MaximumSize = new Size(760, 0);
            inner.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            rowPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.AccentSecondary, 1);
                var rect = new Rectangle(0, 0, rowPanel.Width - 1, rowPanel.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            };

            grid.Controls.Add(rowPanel, 0, row);

            if (addSpacer)
            {
                int spacerRow = grid.RowCount++;
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
                var spacer = new Panel
                {
                    Height = 6,
                    Margin = new Padding(0),
                    Dock = DockStyle.Fill
                };
                grid.Controls.Add(spacer, 0, spacerRow);
            }
        }

        var reflexCombo = MakeCombo(new[]
        {
            (Tr("reflex_off"), 0),
            (Tr("reflex_on"), 1),
            (Tr("reflex_boost"), 2)
        }, snapshot.LatencyMode);
        AddRow(Tr("reflex_label"), reflexCombo);

        var resPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            Margin = new Padding(0, 0, 0, 8)
        };

        var resX = new NumericUpDown
        {
            Minimum = 640,
            Maximum = 7680,
            Value = Math.Max(640, Math.Min(snapshot.Width, 7680)),
            Width = 100,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.TextPrimary
        };
        var resY = new NumericUpDown
        {
            Minimum = 360,
            Maximum = 4320,
            Value = Math.Max(360, Math.Min(snapshot.Height, 4320)),
            Width = 100,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.TextPrimary
        };
        var resSeparator = new Label
        {
            AutoSize = true,
            Text = "x",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.BodyBold,
            ForeColor = Theme.TextPrimary
        };
        var resSeparatorHost = new Panel
        {
            AutoSize = false,
            Width = 20,
            Height = Math.Max(resX.Height, resSeparator.PreferredHeight),
            Margin = new Padding(6, -8, 6, 0),
            BackColor = Theme.Surface
        };
        void LayoutSeparator(object? _, EventArgs __)
        {
            resSeparatorHost.Height = Math.Max(resX.Height, resSeparator.PreferredHeight);
            resSeparator.Left = Math.Max(0, (resSeparatorHost.Width - resSeparator.Width) / 2);
            resSeparator.Top = Math.Max(0, (resSeparatorHost.Height - resSeparator.Height) / 2 - 2);
        }
        resSeparatorHost.Controls.Add(resSeparator);
        resSeparatorHost.Resize += LayoutSeparator;
        LayoutSeparator(null, EventArgs.Empty);
        resPanel.Controls.Add(resX);
        resPanel.Controls.Add(resSeparatorHost);
        resPanel.Controls.Add(resY);
        AddRow(Tr("resolution_label"), resPanel);

        var fsCombo = MakeCombo(new[]
        {
            (Tr("fullscreen"), 0),
            (Tr("windowed_full"), 1),
            (Tr("windowed"), 2)
        }, snapshot.FullscreenMode);
        AddRow(Tr("display_mode_label"), fsCombo);

        var fpsPresets = new (string text, float value)[]
        {
            (Tr("fps_60"), 60f), (Tr("fps_120"), 120f), (Tr("fps_144"), 144f), (Tr("fps_165"), 165f),
            (Tr("fps_180"), 180f), (Tr("fps_240"), 240f), (Tr("fps_360"), 360f),
            (Tr("fps_unlimited"), 0f), (Tr("fps_custom"), -1f)
        };
        int fpsIndex = fpsPresets.ToList().FindIndex(p => Math.Abs(p.value - snapshot.FrameRateLimit) < 0.001f);
        if (fpsIndex < 0) fpsIndex = fpsPresets.Length - 1; // custom

        var fpsPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.Surface
        };
        fpsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fpsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fpsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        fpsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var fpsTrack = new TrackBar
        {
            Minimum = 0,
            Maximum = fpsPresets.Length - 1,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = 1,
            Value = fpsIndex,
            BackColor = Theme.Surface,
            AutoSize = false,
            Height = 42,
            MinimumSize = new Size(360, 42),
            MaximumSize = new Size(520, 42),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6)
        };
        var fpsValueLabel = new Label
        {
            AutoSize = true,
            Text = fpsPresets[fpsIndex].text,
            Font = Theme.BodyBold,
            ForeColor = Theme.TextPrimary,
            Margin = new Padding(0, 0, 12, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        var fpsCustom = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 1000,
            Value = fpsIndex == fpsPresets.Length - 1
                ? Math.Max(0, Math.Min((decimal)snapshot.FrameRateLimit, 1000))
                : (decimal)Math.Max(0, fpsPresets[fpsIndex].value),
            Width = 90,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.TextPrimary,
            Enabled = fpsIndex == fpsPresets.Length - 1,
            TextAlign = HorizontalAlignment.Right,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(10, 0, 0, 0)
        };
        void SyncFpsLabel()
        {
            var preset = fpsPresets[fpsTrack.Value];
            fpsValueLabel.Text = preset.text;
            bool isCustom = Math.Abs(preset.value + 1f) < 0.001f;
            fpsCustom.Enabled = isCustom;
            if (!isCustom)
            {
                fpsCustom.Value = (decimal)Math.Max(0, preset.value);
            }
        }
        fpsTrack.ValueChanged += (_, _) => SyncFpsLabel();
        fpsCustom.ValueChanged += (_, _) =>
        {
            if (fpsTrack.Value != fpsPresets.Length - 1) return;
            fpsValueLabel.Text = $"{Tr("fps_custom")}: {fpsCustom.Value}";
        };
        fpsPanel.Controls.Add(fpsTrack, 0, 0);
        fpsPanel.SetColumnSpan(fpsTrack, 2);
        fpsPanel.Controls.Add(fpsValueLabel, 0, 1);
        fpsPanel.Controls.Add(fpsCustom, 1, 1);
        fpsPanel.Margin = new Padding(0, 0, 0, 4);
        SyncFpsLabel();
        AddRow(Tr("fps_label"), fpsPanel, 16);

        var qualityPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.Surface
        };
        qualityPanel.Dock = DockStyle.Fill;
        qualityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        qualityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        qualityPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        qualityPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var qualityRowHost = new Panel
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 0),
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Theme.Surface
        };

        var qualityTrack = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 5,
            SmallChange = 1,
            LargeChange = 5,
            BackColor = Theme.Surface,
            AutoSize = false,
            Height = 42,
            MinimumSize = new Size(360, 42),
            MaximumSize = new Size(520, 42),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6)
        };

        var quality = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100,
            Value = Math.Max(0, Math.Min(snapshot.ResolutionQuality, 100)),
            Width = 80,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.TextPrimary,
            TextAlign = HorizontalAlignment.Right,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(10, 0, 0, 0)
        };
        var qualityValue = new Label
        {
            AutoSize = true,
            Text = $"{quality.Value}%",
            Font = Theme.BodyBold,
            ForeColor = Theme.TextPrimary,
            Margin = new Padding(0, 0, 12, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        void SyncQualityFromTrack()
        {
            int v = qualityTrack.Value;
            if (quality.Value != v) quality.Value = v;
            qualityValue.Text = $"{v}%";
        }

        void SyncQualityFromNumeric()
        {
            int v = (int)quality.Value;
            if (qualityTrack.Value != v)
            {
                qualityTrack.Value = Math.Max(qualityTrack.Minimum, Math.Min(qualityTrack.Maximum, v));
            }
            qualityValue.Text = $"{v}%";
        }

        qualityTrack.ValueChanged += (_, _) => SyncQualityFromTrack();
        quality.ValueChanged += (_, _) => SyncQualityFromNumeric();

        qualityPanel.Controls.Add(qualityTrack, 0, 0);
        qualityPanel.SetColumnSpan(qualityTrack, 2);
        qualityPanel.Controls.Add(qualityValue, 0, 1);
        qualityPanel.Controls.Add(quality, 1, 1);
        qualityPanel.Margin = new Padding(0, 0, 0, 4);

        qualityRowHost.Controls.Add(qualityPanel);
        qualityPanel.Dock = DockStyle.Fill;

        SyncQualityFromNumeric();

        AddRow(Tr("resolution_scale_label"), qualityRowHost, 16);

        RadioButton MakeRadio(string text)
        {
            return new RadioButton
            {
                Text = text,
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Margin = new Padding(12, 4, 12, 4)
            };
        }

        var dxPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(0, 0, 0, 6),
            Dock = DockStyle.Top
        };
        dxPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dxPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dxPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        dxPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var dx11Perf = MakeRadio(Tr("dx11_perf_adv"));
        var dx11Def = MakeRadio(Tr("dx11_default"));
        var dx12Perf = MakeRadio(Tr("dx12_perf_adv"));
        var dx12Def = MakeRadio(Tr("dx12_default"));

        dx11Perf.Anchor = AnchorStyles.Left;
        dx11Def.Anchor = AnchorStyles.Left;
        dx12Perf.Anchor = AnchorStyles.Left;
        dx12Def.Anchor = AnchorStyles.Left;

        dxPanel.Controls.Add(dx11Perf, 0, 0);
        dxPanel.Controls.Add(dx11Def, 0, 1);
        dxPanel.Controls.Add(dx12Perf, 1, 0);
        dxPanel.Controls.Add(dx12Def, 1, 1);

        foreach (Control c in dxPanel.Controls)
        {
            c.Margin = new Padding(2, 2, 2, 2);
        }

        AddRow(Tr("render_api_label"), dxPanel, 18, addSpacer: false);

        void ApplySnapshot(UtilityActions.SettingsSnapshot snap)
        {
            if (!snap.Exists) return;

            void SelectCombo(ComboBox cb, int target)
            {
                for (int i = 0; i < cb.Items.Count; i++)
                {
                    if (cb.Items[i] is ValueTuple<string, int> opt && opt.Item2 == target)
                    {
                        cb.SelectedIndex = i;
                        break;
                    }
                }
            }

            SelectCombo(reflexCombo, snap.LatencyMode);
            resX.Value = Math.Max(resX.Minimum, Math.Min(resX.Maximum, snap.Width));
            resY.Value = Math.Max(resY.Minimum, Math.Min(resY.Maximum, snap.Height));
            SelectCombo(fsCombo, snap.FullscreenMode);

            int presetIndex = fpsPresets.ToList().FindIndex(p => Math.Abs(p.value - snap.FrameRateLimit) < 0.001f);
            if (presetIndex < 0) presetIndex = fpsPresets.Length - 1;
            fpsTrack.Value = Math.Max(fpsTrack.Minimum, Math.Min(fpsTrack.Maximum, presetIndex));
            bool isCustom = presetIndex == fpsPresets.Length - 1;
            var fpsVal = isCustom ? snap.FrameRateLimit : fpsPresets[presetIndex].value;
            fpsCustom.Value = Math.Max(fpsCustom.Minimum, Math.Min(fpsCustom.Maximum, (decimal)Math.Max(0, fpsVal)));
            SyncFpsLabel();

            quality.Value = Math.Max(quality.Minimum, Math.Min(quality.Maximum, snap.ResolutionQuality));
            switch ($"{snap.DxRhi}|{snap.DxFeature}")
            {
                case "dx11|es31":
                    dx11Perf.Checked = true; break;
                case "dx11|sm5":
                    dx11Def.Checked = true; break;
                case "dx12|es31":
                    dx12Perf.Checked = true; break;
                case "dx12|sm6":
                    dx12Def.Checked = true; break;
                default:
                    dx12Perf.Checked = true; break;
            }
        }

        ApplySnapshot(snapshot);

        reinstallButton.Click += async (_, _) =>
        {
            if (File.Exists(UtilityActions.GameUserSettingsPath))
            {
                var overwrite = Dialogs.Confirm(dlg, Tr("confirm_replace_title"),
                    Tr("confirm_replace_body"),
                    Tr("confirm_replace_yes"), Tr("cancel"));
                if (overwrite != DialogResult.Yes) return;
            }

            await RunDialogActionAsync(dlg, async () =>
            {
                var ok = await UtilityActions.DownloadGameUserSettingsAsync();
                if (!ok)
                {
                    Dialogs.Error(dlg, Tr("install_error_title"), WithDetails(Tr("install_error_body")));
                    return;
                }

                Dialogs.Info(dlg, Tr("install_done_title"), Tr("install_done_body"));
                reinstallButton.Text = Tr("reinstall_button");
                ApplySnapshot(UtilityActions.GetSettingsSnapshot());
            });
        };

        var save = CreateAccentButton(Tr("save_button"), Theme.Accent);
        save.MinimumSize = new Size(120, 36);
        var cancel = CreateAccentButton(Tr("cancel_button"), Theme.SurfaceAlt);
        cancel.ForeColor = Theme.TextPrimary;
        cancel.MinimumSize = new Size(120, 36);
        cancel.Click += (_, _) => dlg.Close();
        dlg.CancelButton = cancel;

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Padding = new Padding(0, 2, 0, 0),
            Margin = new Padding(0, 4, 0, 12),
            BackColor = Theme.Surface
        };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        save.Click += async (_, _) =>
        {
            var selReflex = (ValueTuple<string, int>)reflexCombo.SelectedItem!;
            var selFs = (ValueTuple<string, int>)fsCombo.SelectedItem!;
            var fpsPreset = fpsPresets[fpsTrack.Value];
            float fpsValue = Math.Abs(fpsPreset.value + 1f) < 0.001f
                ? (float)fpsCustom.Value
                : fpsPreset.value;
            var (rhi, feature) = dx11Perf.Checked ? ("dx11", "es31")
                : dx11Def.Checked ? ("dx11", "sm5")
                : dx12Def.Checked ? ("dx12", "sm6")
                : ("dx12", "es31");

            var payload = new UtilityActions.AdvancedSettingsPayload
            {
                LatencyMode = selReflex.Item2,
                Width = (int)resX.Value,
                Height = (int)resY.Value,
                FullscreenMode = selFs.Item2,
                FrameRateLimit = fpsValue,
                ResolutionQuality = (int)quality.Value,
                DxRhi = rhi,
                DxFeature = feature
            };

            await RunDialogActionAsync(dlg, async () =>
            {
                var ok = await UtilityActions.ApplyAdvancedSettingsAsync(payload);
                if (!ok)
                {
                    Dialogs.Error(dlg, Tr("advanced_save_failed"), WithDetails(Tr("advanced_save_failed_body")));
                    return;
                }

                Dialogs.Info(dlg, Tr("advanced_saved_title"), Tr("advanced_saved_body"));
                dlg.Close();
            });
        };

        layout.Controls.Add(headerHost, 0, 0);
        layout.Controls.Add(gridHost, 0, 1);
        layout.Controls.Add(buttons, 0, 2);

        layout.PerformLayout();
        var preferred = layout.GetPreferredSize(Size.Empty);
        int clientWidth = Math.Max(640, preferred.Width + 12);
        int clientHeight = Math.Max(520, preferred.Height + 12);
        dlg.ClientSize = new Size(clientWidth, clientHeight);
        dlg.MinimumSize = dlg.ClientSize;

        dlg.Controls.Add(layout);
        dlg.ResumeLayout(false);
        dlg.PerformLayout();
        dlg.ShowDialog(this);
    }

    private async Task ApplyPresetAsync(Form dialog, string rhi, string feature, bool requireBase, string successText)
    {
        await RunDialogActionAsync(dialog, async () =>
        {
            var ok = await UtilityActions.WriteGusValuesAsync(rhi, feature, requireBase);
            if (!ok)
            {
                Dialogs.Error(dialog, Tr("modify_error_title"), WithDetails(Tr("modify_error_body")));
                return;
            }

            Dialogs.Info(dialog, Tr("done_title"), successText);
            dialog.Close();
        });
    }

    private static async Task RunDialogActionAsync(Form dialog, Func<Task> action)
    {
        try
        {
            dialog.Enabled = false;
            dialog.UseWaitCursor = true;
            await action();
        }
        finally
        {
            dialog.Enabled = true;
            dialog.UseWaitCursor = false;
        }
    }

    private async Task<bool> RunWithResultAsync(string status, Func<Task<bool>> action)
    {
        bool ok = false;
        await RunWithBusyState(async () =>
        {
            ok = await action();
        }, status);
        return ok;
    }

    private async Task RunWithBusyState(Func<Task> action, string status)
    {
        try
        {
            SetBusy(true, status);
            await action();
        }
        finally
        {
            SetBusy(false, Tr("status_ready"));
        }
    }

    private void SetBusy(bool busy, string status)
    {
        UseWaitCursor = busy;
        var enabled = !busy;
        if (_clearButton != null) _clearButton.Enabled = enabled;
        if (_graphicsButton != null) _graphicsButton.Enabled = enabled;
        if (_autoSetupButton != null) _autoSetupButton.Enabled = enabled;
        if (_advancedButton != null) _advancedButton.Enabled = enabled;
        if (_backupButton != null) _backupButton.Enabled = enabled;
        if (_restoreButton != null) _restoreButton.Enabled = enabled;
    }

    private async Task CheckForUpdatesAsync()
    {
        var (remote, snapshot) = await VersionManager.CheckForUpdateAsync(_softCheck);
        _versionSnapshot = snapshot;
        UpdateVersionLabel(snapshot.Version);

        if (VersionManager.IsUpdateAvailable(remote, snapshot))
        {
            var remoteVersion = remote!.Trim();
            var open = Dialogs.Confirm(this, Tr("update_available_title"),
                string.Format(Tr("update_available_body"), remoteVersion),
                Tr("update_open"), Tr("update_later"));

            if (open == DialogResult.Yes)
            {
                TryOpenUrl(VersionManager.ReleaseUrl);
            }
            else
            {
                var skip = Dialogs.Confirm(this, Tr("skip_version_title"),
                    string.Format(Tr("skip_version_body"), remoteVersion),
                    Tr("skip_yes"), Tr("skip_no"));
                if (skip == DialogResult.Yes)
                {
                    VersionManager.RecordSkip(remoteVersion, snapshot);
                }
                else
                {
                    VersionManager.RecordInstalled(snapshot.Version, snapshot);
                }
            }
        }
    }

    private void UpdateVersionLabel(string version)
    {
        _versionLink.Text = $"{Tr("version_prefix")}: {version}";
    }

    private void ToggleLanguage()
    {
        _lang = _lang == Language.En ? Language.Ru : Language.En;
        BuildCards();
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        if (_subtitleLabel != null) _subtitleLabel.Text = Tr("subtitle");
        if (_clearTitleLabel != null) _clearTitleLabel.Text = Tr("clear_title");
        if (_clearDescLabel != null) _clearDescLabel.Text = Tr("clear_desc");
        if (_clearButton != null) _clearButton.Text = Tr("clear_button");
        if (_backupButton != null) _backupButton.Text = Tr("backup_button");
        if (_restoreButton != null) _restoreButton.Text = Tr("restore_button");
        if (_installTitleLabel != null) _installTitleLabel.Text = Tr("install_title");
        if (_installDescLabel != null) _installDescLabel.Text = Tr("install_desc");
        if (_graphicsButton != null) _graphicsButton.Text = Tr("auto_setup");
        if (_autoSetupButton != null) _autoSetupButton.Text = Tr("auto_setup");
        if (_advancedButton != null) _advancedButton.Text = Tr("advanced_settings");
        if (_langButton != null) _langButton.Text = _lang == Language.En ? "RU" : "EN";
        UpdateVersionLabel(_versionSnapshot.Version);
        if (_bodyPanel != null) CenterStack(_bodyPanel);
        if (_langButton?.Parent is Control header)
        {
            _langButton.Left = header.ClientSize.Width - _langButton.Width - 12;
        }
    }

    private static void TryOpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore
        }
    }
}
