using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Markdig;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace NeonWiki
{
    public partial class MainWindow : Window
    {
        private string _currentWikiPath = string.Empty;
        private Dictionary<TabItem, WebView2> _webViews = new Dictionary<TabItem, WebView2>();
        private Dictionary<TabItem, TextBox> _editors = new Dictionary<TabItem, TextBox>();
        private Dictionary<TabItem, string> _filePaths = new Dictionary<TabItem, string>();
        private Dictionary<TabItem, bool> _isModified = new Dictionary<TabItem, bool>();

        // NUEVA: Carpeta temporal para HTMLs
        private string _tempHtmlFolder;
        // NUEVA: Carpeta de datos de usuario para WebView2
        private string _webView2UserDataFolder;

        public MainWindow()
        {
            InitializeComponent();

            // Crear carpeta temporal para HTMLs
            _tempHtmlFolder = Path.Combine(Path.GetTempPath(), "NeonWiki", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempHtmlFolder);

            // Crear carpeta de datos de usuario para WebView2 en AppData Local
            _webView2UserDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NeonWiki",
                "WebView2",
                "UserData"
            );
            Directory.CreateDirectory(_webView2UserDataFolder);

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StatusBar.Text = "⚡ NEON WIKI READY";
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Limpiar carpeta temporal
            try
            {
                if (Directory.Exists(_tempHtmlFolder))
                {
                    Directory.Delete(_tempHtmlFolder, true);
                }
            }
            catch { }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Wiki Folder",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _currentWikiPath = dialog.SelectedPath;
                LoadWikiStructure();
                StatusBar.Text = $"📁 Loaded: {Path.GetFileName(_currentWikiPath)}";
            }
        }

        private void LoadWikiStructure()
        {
            TreeViewFiles.Items.Clear();

            if (!Directory.Exists(_currentWikiPath)) return;

            var rootItem = new TreeViewItem
            {
                Header = $"📁 {Path.GetFileName(_currentWikiPath)}",
                Tag = _currentWikiPath,
                IsExpanded = true,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 255))
            };

            LoadDirectory(rootItem, _currentWikiPath);
            TreeViewFiles.Items.Add(rootItem);
        }

        private void LoadDirectory(TreeViewItem parent, string path)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirItem = new TreeViewItem
                    {
                        Header = $"📁 {Path.GetFileName(dir)}",
                        Tag = dir,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 200))
                    };
                    LoadDirectory(dirItem, dir);
                    parent.Items.Add(dirItem);
                }

                foreach (var file in Directory.GetFiles(path, "*.md"))
                {
                    var fileItem = new TreeViewItem
                    {
                        Header = $"📄 {Path.GetFileName(file)}",
                        Tag = file,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 180, 255))
                    };
                    parent.Items.Add(fileItem);
                }
            }
            catch (Exception ex)
            {
                StatusBar.Text = $"❌ Error: {ex.Message}";
            }
        }

        private void TreeViewFiles_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TreeViewFiles.SelectedItem is TreeViewItem item && item.Tag is string path)
            {
                if (File.Exists(path) && path.EndsWith(".md"))
                {
                    OpenFileInTab(path);
                }
            }
        }

        private void OpenFileInTab(string filePath)
        {
            // Verificar si ya está abierto
            var existingTab = _filePaths.FirstOrDefault(x => x.Value == filePath).Key;
            if (existingTab != null)
            {
                TabControlMain.SelectedItem = existingTab;
                return;
            }

            // Crear nuevo tab principal
            var fileTab = new TabItem
            {
                Header = Path.GetFileName(filePath),
                Style = (Style)FindResource("NeonTabItemStyle")
            };

            // TabControl interno para VIEW/EDIT
            var innerTabControl = new TabControl
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                BorderThickness = new Thickness(0)
            };

            // Tab VIEW con barra de búsqueda y WebView2
            var viewTab = new TabItem
            {
                Header = "VIEW",
                Style = (Style)FindResource("NeonTabItemStyle")
            };

            // Contenedor para barra de búsqueda + WebView2
            var viewContainer = new Grid();
            viewContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50, GridUnitType.Pixel) });
            viewContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Barra de búsqueda
            var searchBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 26, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 255)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var searchPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // TextBox de búsqueda
            var searchTextBox = new TextBox
            {
                Width = 300,
                Height = 30,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Style = (Style)FindResource("NeonTextBoxStyle"),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            // Placeholder text (se mostrará cuando esté vacío)
            searchTextBox.GotFocus += (s, e) =>
            {
                if (searchTextBox.Text == "🔍 Buscar en documento...")
                    searchTextBox.Text = "";
            };
            searchTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(searchTextBox.Text))
                    searchTextBox.Text = "🔍 Buscar en documento...";
            };
            searchTextBox.Text = "🔍 Buscar en documento...";
            searchTextBox.Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128));

            // Botón Buscar
            var searchButton = new Button
            {
                Content = "Buscar",
                Style = (Style)FindResource("NeonButtonStyle"),
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };

            // Botón Siguiente
            var nextButton = new Button
            {
                Content = "▶",
                Style = (Style)FindResource("NeonButtonStyle"),
                Width = 35,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                ToolTip = "Siguiente (F3)"
            };

            // Botón Anterior
            var prevButton = new Button
            {
                Content = "◀",
                Style = (Style)FindResource("NeonButtonStyle"),
                Width = 35,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                ToolTip = "Anterior (Shift+F3)"
            };

            // CheckBox Coincidencia Exacta
            var exactMatchCheckBox = new CheckBox
            {
                Content = "Coincidencia exacta",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 255)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // CheckBox Coincidencia Aproximada (fuzzy)
            var fuzzyMatchCheckBox = new CheckBox
            {
                Content = "Coincidencia aproximada",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 255)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsChecked = true // Por defecto aproximada
            };

            // Label para mostrar resultados
            var resultsLabel = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 255)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            searchPanel.Children.Add(searchTextBox);
            searchPanel.Children.Add(searchButton);
            searchPanel.Children.Add(prevButton);
            searchPanel.Children.Add(nextButton);
            searchPanel.Children.Add(exactMatchCheckBox);
            searchPanel.Children.Add(fuzzyMatchCheckBox);
            searchPanel.Children.Add(resultsLabel);

            searchBar.Child = searchPanel;
            Grid.SetRow(searchBar, 0);
            viewContainer.Children.Add(searchBar);

            var webView = new WebView2
            {
                MinWidth = 100,
                MinHeight = 100
            };

            // Variables para búsqueda
            int currentSearchIndex = -1;
            List<int> searchResults = new List<int>();

            // Función para navegar a resultado (declarada primero)
            Action<int>? navigateToResult = null;
            navigateToResult = async (index) =>
            {
                if (webView.CoreWebView2 == null || index < 0 || index >= searchResults.Count)
                    return;

                try
                {
                    string navigateScript = $@"
                        (function() {{
                            var highlights = document.querySelectorAll('.neon-search-highlight');
                            if (highlights.length > 0) {{
                                // Remover highlight actual
                                highlights.forEach(function(el) {{
                                    el.classList.remove('neon-search-highlight-active');
                                }});

                                var target = highlights[{index}];
                                if (target) {{
                                    target.classList.add('neon-search-highlight-active');
                                    target.scrollIntoView({{ behavior: 'smooth', block: 'center' }});
                                }}
                            }}
                        }})();
                    ";

                    await webView.CoreWebView2.ExecuteScriptAsync(navigateScript);
                }
                catch { }
            };

            // Función para ejecutar búsqueda en WebView2
            Action<string, bool, bool> performSearch = async (searchText, exactMatch, fuzzyMatch) =>
            {
                if (string.IsNullOrWhiteSpace(searchText) || webView.CoreWebView2 == null)
                {
                    resultsLabel.Text = "";
                    return;
                }

                try
                {
                    // Escapar el texto de búsqueda para JavaScript
                    var escapedText = searchText.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"");
                    
                    string searchScript = $@"
                        (function() {{
                            // Remover resaltados anteriores
                            var highlights = document.querySelectorAll('.neon-search-highlight');
                            highlights.forEach(function(el) {{
                                var parent = el.parentNode;
                                parent.replaceChild(document.createTextNode(el.textContent), el);
                                parent.normalize();
                            }});

                            var searchText = '{escapedText}';
                            var exactMatch = {exactMatch.ToString().ToLower()};
                            var fuzzyMatch = {fuzzyMatch.ToString().ToLower()};
                            var results = [];
                            var searchIndex = 0;

                            function escapeRegex(str) {{
                                return str.replace(/[.*+?^${{}}()|[\]\\]/g, '\\$&');
                            }}

                            function createHighlight(text) {{
                                var span = document.createElement('span');
                                span.className = 'neon-search-highlight';
                                span.textContent = text;
                                return span;
                            }}

                            function searchInNode(node) {{
                                if (node.nodeType === Node.TEXT_NODE) {{
                                    var text = node.textContent;
                                    var parent = node.parentNode;
                                    
                                    // No buscar en código
                                    if (parent.tagName === 'CODE' || parent.tagName === 'PRE' || 
                                        parent.closest('pre') !== null || parent.closest('code') !== null) {{
                                        return;
                                    }}

                                    var pattern;
                                    if (exactMatch) {{
                                        pattern = new RegExp(escapeRegex(searchText), 'gi');
                                    }} else if (fuzzyMatch) {{
                                        // Búsqueda aproximada: permite variaciones
                                        var fuzzyPattern = searchText.split('').map(function(char) {{
                                            return escapeRegex(char);
                                        }}).join('.*?');
                                        pattern = new RegExp(fuzzyPattern, 'gi');
                                    }} else {{
                                        pattern = new RegExp(escapeRegex(searchText), 'gi');
                                    }}

                                    var matches = [];
                                    var match;
                                    while ((match = pattern.exec(text)) !== null) {{
                                        matches.push({{
                                            index: match.index,
                                            length: match[0].length,
                                            text: match[0]
                                        }});
                                    }}

                                    if (matches.length > 0) {{
                                        var fragment = document.createDocumentFragment();
                                        var lastIndex = 0;

                                        matches.forEach(function(match) {{
                                            // Texto antes del match
                                            if (match.index > lastIndex) {{
                                                fragment.appendChild(document.createTextNode(text.substring(lastIndex, match.index)));
                                            }}

                                            // Highlight
                                            var highlight = createHighlight(match.text);
                                            highlight.setAttribute('data-search-index', searchIndex++);
                                            fragment.appendChild(highlight);
                                            results.push(highlight);

                                            lastIndex = match.index + match.length;
                                        }});

                                        // Texto restante
                                        if (lastIndex < text.length) {{
                                            fragment.appendChild(document.createTextNode(text.substring(lastIndex)));
                                        }}

                                        parent.replaceChild(fragment, node);
                                    }}
                                }} else if (node.nodeType === Node.ELEMENT_NODE) {{
                                    // No buscar en código, scripts, estilos
                                    if (node.tagName === 'CODE' || node.tagName === 'PRE' || 
                                        node.tagName === 'SCRIPT' || node.tagName === 'STYLE' ||
                                        node.classList.contains('mermaid')) {{
                                        return;
                                    }}

                                    var children = Array.from(node.childNodes);
                                    children.forEach(function(child) {{
                                        searchInNode(child);
                                    }});
                                }}
                            }}

                            searchInNode(document.body);
                            
                            return {{
                                count: results.length,
                                results: results.map(function(r, i) {{ return i; }})
                            }};
                        }})();
                    ";

                    var result = await webView.CoreWebView2.ExecuteScriptAsync(searchScript);
                    
                    // Obtener el número de resultados directamente
                    var countScript = @"(function() {
                        return document.querySelectorAll('.neon-search-highlight').length;
                    })();";
                    
                    var countResult = await webView.CoreWebView2.ExecuteScriptAsync(countScript);
                    var countStr = countResult.Trim().Trim('"');
                    
                    if (int.TryParse(countStr, out int count))
                    {
                        resultsLabel.Text = count > 0 ? $"Encontrados: {count}" : "No encontrado";
                        currentSearchIndex = -1;
                        searchResults.Clear();
                        for (int i = 0; i < count; i++)
                        {
                            searchResults.Add(i);
                        }
                        
                        // Si hay resultados, navegar al primero
                        if (count > 0 && navigateToResult != null)
                        {
                            currentSearchIndex = 0;
                            navigateToResult(0);
                            resultsLabel.Text = $"Encontrados: {count} (1/{count})";
                        }
                    }
                    else
                    {
                        resultsLabel.Text = "Error en búsqueda";
                    }
                }
                catch (Exception ex)
                {
                    resultsLabel.Text = $"Error: {ex.Message}";
                }
            };

            // Eventos de búsqueda
            searchButton.Click += (s, e) =>
            {
                performSearch(searchTextBox.Text, exactMatchCheckBox.IsChecked == true, fuzzyMatchCheckBox.IsChecked == true);
            };

            searchTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    performSearch(searchTextBox.Text, exactMatchCheckBox.IsChecked == true, fuzzyMatchCheckBox.IsChecked == true);
                    e.Handled = true;
                }
            };

            nextButton.Click += (s, e) =>
            {
                if (searchResults.Count > 0)
                {
                    currentSearchIndex = (currentSearchIndex + 1) % searchResults.Count;
                    navigateToResult(currentSearchIndex);
                    resultsLabel.Text = $"Encontrados: {searchResults.Count} ({currentSearchIndex + 1}/{searchResults.Count})";
                }
            };

            prevButton.Click += (s, e) =>
            {
                if (searchResults.Count > 0)
                {
                    currentSearchIndex = (currentSearchIndex - 1 + searchResults.Count) % searchResults.Count;
                    navigateToResult(currentSearchIndex);
                    resultsLabel.Text = $"Encontrados: {searchResults.Count} ({currentSearchIndex + 1}/{searchResults.Count})";
                }
            };

            // Atajos de teclado globales para búsqueda
            this.KeyDown += (s, e) =>
            {
                if (innerTabControl.SelectedItem == viewTab && searchTextBox.IsFocused == false)
                {
                    if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.None)
                    {
                        nextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                    }
                    else if (e.Key == Key.F3 && Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        prevButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                    }
                    else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        searchTextBox.Focus();
                        searchTextBox.SelectAll();
                        e.Handled = true;
                    }
                }
            };

            webView.CoreWebView2InitializationCompleted += async (s, args) =>
            {
                if (args.IsSuccess)
                {
                    // NUEVO: Configurar mapeo de carpeta virtual
                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "neonwiki.local",
                        _tempHtmlFolder,
                        CoreWebView2HostResourceAccessKind.Allow
                    );

                    await RenderHtmlForTab(fileTab, filePath);
                }
                else
                {
                    StatusBar.Text = $"❌ WebView2 initialization failed: {args.InitializationException?.Message}";
                }
            };

            Grid.SetRow(webView, 1);
            viewContainer.Children.Add(webView);
            viewTab.Content = viewContainer;
            innerTabControl.Items.Add(viewTab);
            
            // Seleccionar VIEW por defecto para que WebView2 tenga tamaño válido
            innerTabControl.SelectedItem = viewTab;

            // Tab EDIT con Toolbar y TextBox
            var editTab = new TabItem
            {
                Header = "EDIT",
                Style = (Style)FindResource("NeonTabItemStyle")
            };

            // Contenedor vertical para toolbar + editor
            var editContainer = new Grid();
            editContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50, GridUnitType.Pixel) });
            editContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Toolbar de formato
            var toolbar = CreateMarkdownToolbar(fileTab);
            Grid.SetRow(toolbar, 0);
            editContainer.Children.Add(toolbar);

            var editor = new TextBox
            {
                Style = (Style)FindResource("NeonTextBoxStyle"),
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Text = File.ReadAllText(filePath)
            };
            Grid.SetRow(editor, 1);
            editContainer.Children.Add(editor);

            editor.TextChanged += async (s, e) =>
            {
                MarkAsModified(fileTab);

                // Actualizar HTML si estamos en VIEW (pero solo si WebView2 está listo)
                if (innerTabControl.SelectedItem == viewTab && _webViews.ContainsKey(fileTab))
                {
                    var wv = _webViews[fileTab];
                    if (wv.CoreWebView2 != null)
                    {
                        await RenderHtmlForTab(fileTab, filePath, editor.Text);
                    }
                }
            };

            editTab.Content = editContainer;
            innerTabControl.Items.Add(editTab);

            // Al cambiar de tab, actualizar HTML o inicializar WebView2 si es necesario
            innerTabControl.SelectionChanged += async (s, e) =>
            {
                if (innerTabControl.SelectedItem == viewTab && _editors.ContainsKey(fileTab) && _webViews.ContainsKey(fileTab))
                {
                    var wv = _webViews[fileTab];
                    
                    // Si WebView2 no está inicializado, inicializarlo ahora
                    if (wv.CoreWebView2 == null)
                    {
                        try
                        {
                            // Crear CoreWebView2Environment con UserDataFolder personalizado
                            var env = await CoreWebView2Environment.CreateAsync(
                                userDataFolder: _webView2UserDataFolder
                            );
                            
                            // Inicializar WebView2 con el environment personalizado
                            await wv.EnsureCoreWebView2Async(env);
                        }
                        catch (Exception ex)
                        {
                            StatusBar.Text = $"❌ WebView2 init error: {ex.Message}";
                            return;
                        }
                    }
                    
                    // Renderizar el contenido
                    await RenderHtmlForTab(fileTab, filePath, _editors[fileTab].Text);
                }
            };

            fileTab.Content = innerTabControl;

            // Guardar referencias ANTES de agregar al TabControl
            _webViews[fileTab] = webView;
            _editors[fileTab] = editor;
            _filePaths[fileTab] = filePath;
            _isModified[fileTab] = false;

            TabControlMain.Items.Add(fileTab);
            TabControlMain.SelectedItem = fileTab;

            // Inicializar WebView2 DESPUÉS de agregar al árbol visual
            // Usar Dispatcher para asegurar que se ejecute después del renderizado
            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // Esperar un momento para que el UI se renderice completamente
                    await System.Threading.Tasks.Task.Delay(100);
                    
                    if (webView.CoreWebView2 == null)
                    {
                        // Crear CoreWebView2Environment con UserDataFolder personalizado
                        // Esto evita el error de permisos de acceso
                        var env = await CoreWebView2Environment.CreateAsync(
                            userDataFolder: _webView2UserDataFolder
                        );
                        
                        // Inicializar WebView2 con el environment personalizado
                        await webView.EnsureCoreWebView2Async(env);
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => StatusBar.Text = $"❌ WebView2 init error: {ex.Message}");
                }
            }, System.Windows.Threading.DispatcherPriority.Loaded);

            StatusBar.Text = $"📝 Opened: {Path.GetFileName(filePath)}";
        }

        // NUEVO: Método mejorado con archivos HTML temporales
        private async System.Threading.Tasks.Task RenderHtmlForTab(TabItem fileTab, string filePath, string? markdownText = null)
        {
            if (!_webViews.ContainsKey(fileTab)) return;

            try
            {
                // Verificar que la carpeta temporal esté inicializada
                if (string.IsNullOrEmpty(_tempHtmlFolder) || !Directory.Exists(_tempHtmlFolder))
                {
                    _tempHtmlFolder = Path.Combine(Path.GetTempPath(), "NeonWiki", Guid.NewGuid().ToString());
                    Directory.CreateDirectory(_tempHtmlFolder);
                }

                var webView = _webViews[fileTab];
                
                // Verificar que WebView2 esté inicializado antes de renderizar
                if (webView.CoreWebView2 == null)
                {
                    // Crear CoreWebView2Environment con UserDataFolder personalizado
                    var env = await CoreWebView2Environment.CreateAsync(
                        userDataFolder: _webView2UserDataFolder
                    );
                    
                    // Esperar a que WebView2 se inicialice con el environment personalizado
                    await webView.EnsureCoreWebView2Async(env);
                }

                // Verificar nuevamente después de EnsureCoreWebView2Async
                if (webView.CoreWebView2 == null)
                {
                    StatusBar.Text = $"⏳ Waiting for WebView2 initialization...";
                    return;
                }

                // Asegurar que el mapeo del dominio virtual esté configurado
                try
                {
                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "neonwiki.local",
                        _tempHtmlFolder,
                        CoreWebView2HostResourceAccessKind.Allow
                    );
                }
                catch
                {
                    // El mapeo ya puede estar configurado, ignorar error
                }

                var markdown = markdownText ?? File.ReadAllText(filePath);

                // Generar HTML
                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                var htmlContent = Markdown.ToHtml(markdown, pipeline);

                // Procesar bloques de código Mermaid para convertir en divs de Mermaid
                htmlContent = ProcessMermaidBlocks(htmlContent);

                var fullHtml = GetNeonHtmlTemplate(htmlContent);

                // NUEVA TÉCNICA: Guardar HTML en archivo temporal
                var htmlFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{fileTab.GetHashCode()}.html";
                var htmlFilePath = Path.Combine(_tempHtmlFolder, htmlFileName);

                await File.WriteAllTextAsync(htmlFilePath, fullHtml, System.Text.Encoding.UTF8);

                // Verificar que el archivo se creó correctamente
                if (!File.Exists(htmlFilePath))
                {
                    StatusBar.Text = $"❌ HTML file not created: {htmlFilePath}";
                    return;
                }

                // Esperar un momento para asegurar que el archivo esté completamente escrito
                await System.Threading.Tasks.Task.Delay(100);

                // Navegar usando URI local virtual
                var uri = $"https://neonwiki.local/{htmlFileName}";
                
                // Verificar que la navegación sea posible
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Navigate(uri);
                    
                    // Esperar un momento y verificar que la navegación se completó
                    await System.Threading.Tasks.Task.Delay(200);
                    
                    StatusBar.Text = $"✅ Rendered: {Path.GetFileName(filePath)}";
                }
                else
                {
                    StatusBar.Text = $"❌ WebView2 is null, cannot navigate";
                }
            }
            catch (Exception ex)
            {
                StatusBar.Text = $"❌ Render error: {ex.Message}";
            }
        }

        private void MarkAsModified(TabItem tab)
        {
            if (!_isModified.ContainsKey(tab) || _isModified[tab]) return;

            _isModified[tab] = true;
            var fileName = Path.GetFileName(_filePaths[tab]);
            tab.Header = $"{fileName} *";
        }

        private void SaveFile(TabItem tab)
        {
            if (!_editors.ContainsKey(tab) || !_filePaths.ContainsKey(tab)) return;

            try
            {
                File.WriteAllText(_filePaths[tab], _editors[tab].Text);
                _isModified[tab] = false;
                tab.Header = Path.GetFileName(_filePaths[tab]);
                StatusBar.Text = $"💾 Saved: {Path.GetFileName(_filePaths[tab])}";
            }
            catch (Exception ex)
            {
                StatusBar.Text = $"❌ Save error: {ex.Message}";
            }
        }

        private void SaveCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (TabControlMain.SelectedItem is TabItem tab && _editors.ContainsKey(tab))
            {
                SaveFile(tab);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (TabControlMain.SelectedItem is TabItem tab && _editors.ContainsKey(tab))
            {
                if (_isModified.ContainsKey(tab) && _isModified[tab])
                {
                    var result = MessageBox.Show(
                        $"Save changes to {Path.GetFileName(_filePaths[tab])}?",
                        "Unsaved Changes",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveFile(tab);
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                // Limpiar archivo HTML temporal
                try
                {
                    var htmlFileName = $"{Path.GetFileNameWithoutExtension(_filePaths[tab])}_{tab.GetHashCode()}.html";
                    var htmlFilePath = Path.Combine(_tempHtmlFolder, htmlFileName);
                    if (File.Exists(htmlFilePath))
                    {
                        File.Delete(htmlFilePath);
                    }
                }
                catch { }

                _webViews.Remove(tab);
                _editors.Remove(tab);
                _filePaths.Remove(tab);
                _isModified.Remove(tab);
                TabControlMain.Items.Remove(tab);
            }
        }

        // Obtener editor actual del tab seleccionado
        private TextBox? GetCurrentEditor()
        {
            if (TabControlMain.SelectedItem is TabItem tab && _editors.ContainsKey(tab))
            {
                return _editors[tab];
            }
            return null;
        }

        // Insertar o envolver texto en el editor
        private void InsertOrWrapText(string prefix, string suffix, string? placeholder = null)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var selectionStart = editor.SelectionStart;
            var selectionLength = editor.SelectionLength;
            var text = editor.Text;

            if (selectionLength > 0)
            {
                // Hay texto seleccionado: envolver
                var selectedText = text.Substring(selectionStart, selectionLength);
                
                // Verificar si ya está envuelto (toggle)
                if (IsWrappedWith(selectedText, prefix, suffix))
                {
                    // Quitar el formato (toggle off)
                    var unwrapped = selectedText.Substring(prefix.Length, selectedText.Length - prefix.Length - suffix.Length);
                    text = text.Remove(selectionStart, selectionLength);
                    text = text.Insert(selectionStart, unwrapped);
                    editor.Text = text;
                    editor.SelectionStart = selectionStart;
                    editor.SelectionLength = unwrapped.Length;
                }
                else
                {
                    // Envolver con formato
                    var wrapped = prefix + selectedText + suffix;
                    text = text.Remove(selectionStart, selectionLength);
                    text = text.Insert(selectionStart, wrapped);
                    editor.Text = text;
                    editor.SelectionStart = selectionStart + wrapped.Length;
                    editor.SelectionLength = 0;
                }
            }
            else
            {
                // No hay selección: insertar plantilla
                var template = prefix + (placeholder ?? "") + suffix;
                text = text.Insert(selectionStart, template);
                editor.Text = text;
                editor.SelectionStart = selectionStart + prefix.Length;
                editor.SelectionLength = placeholder?.Length ?? 0;
            }

            editor.Focus();
        }

        // Verificar si el texto está envuelto con prefijo/sufijo
        private bool IsWrappedWith(string text, string prefix, string suffix)
        {
            return text.StartsWith(prefix) && text.EndsWith(suffix) && text.Length >= prefix.Length + suffix.Length;
        }

        // Reemplazar texto en la posición especificada
        private void ReplaceText(int start, int length, string replacement)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            editor.Text = editor.Text.Remove(start, length).Insert(start, replacement);
            editor.SelectionStart = start + replacement.Length;
            editor.SelectionLength = 0;
            editor.Focus();
        }

        // Crear toolbar de formato Markdown
        private Border CreateMarkdownToolbar(TabItem fileTab)
        {
            var toolbar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 26, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 255)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Grupo 1: Formato básico
            stackPanel.Children.Add(CreateToolbarButton("B", "Negrita (Ctrl+B)", () => InsertOrWrapText("**", "**", "texto")));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("I", "Cursiva (Ctrl+I)", () => InsertOrWrapText("*", "*", "texto")));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("S", "Tachado (Alt+S)", () => InsertOrWrapText("~~", "~~", "texto")));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("`", "Código inline (Ctrl+`)", () => InsertOrWrapText("`", "`", "código")));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("```", "Bloque código (Ctrl+Shift+C)", () => InsertCodeBlock()));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("\"", "Cita (Ctrl+Shift+Q)", () => InsertOrWrapText("> ", "")));

            // Grupo 2: Encabezados (dropdown)
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateHeadingDropdown(fileTab));

            // Grupo 3: Listas
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("•", "Lista viñetas (Ctrl+Shift+L)", () => InsertOrWrapText("- ", "")));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("1.", "Lista numerada (Ctrl+Shift+N)", () => InsertOrWrapText("1. ", "")));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("☐", "Checklist (Ctrl+Shift+T)", () => InsertOrWrapText("- [ ] ", "")));

            // Grupo 4: Elementos estructurales
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("─", "Separador (Ctrl+Shift+H)", () => InsertHorizontalRule()));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("🗃", "Tabla (Ctrl+Shift+T)", () => InsertTable()));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("🖼", "Imagen (Ctrl+Shift+I)", () => InsertImage()));
            stackPanel.Children.Add(CreateToolbarSeparator());
            stackPanel.Children.Add(CreateToolbarButton("🔗", "Link (Ctrl+K)", () => InsertLink()));

            toolbar.Child = stackPanel;
            return toolbar;
        }

        // Crear botón de toolbar
        private Button CreateToolbarButton(string content, string tooltip, Action onClick)
        {
            var button = new Button
            {
                Content = content,
                Style = (Style)FindResource("NeonToolbarButtonStyle"),
                Width = 35,
                Height = 35,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                ToolTip = tooltip
            };

            button.Click += (s, e) => onClick();
            return button;
        }

        // Crear separador visual
        private Border CreateToolbarSeparator()
        {
            return new Border
            {
                Width = 1,
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(0, 128, 128)),
                Margin = new Thickness(5, 0, 5, 0)
            };
        }

        // Crear dropdown para encabezados
        private Button CreateHeadingDropdown(TabItem fileTab)
        {
            var button = new Button
            {
                Content = "#",
                Style = (Style)FindResource("NeonToolbarButtonStyle"),
                Width = 35,
                Height = 35,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                ToolTip = "Encabezados (Ctrl+1-6)"
            };

            button.Click += (s, e) =>
            {
                var contextMenu = new ContextMenu
                {
                    Background = new SolidColorBrush(Color.FromRgb(0, 26, 26)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 255, 255)),
                    BorderThickness = new Thickness(1)
                };

                for (int i = 1; i <= 6; i++)
                {
                    var menuItem = new MenuItem
                    {
                        Header = $"H{i} - {new string('#', i)} Título",
                        Style = (Style)FindResource("NeonMenuItemStyle")
                    };
                    var level = i;
                    menuItem.Click += (s2, e2) => InsertHeading(level);
                    contextMenu.Items.Add(menuItem);
                }

                button.ContextMenu = contextMenu;
                contextMenu.IsOpen = true;
            };

            return button;
        }

        // Métodos específicos de formato
        private void InsertCodeBlock()
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var selectionStart = editor.SelectionStart;
            var selectionLength = editor.SelectionLength;
            var text = editor.Text;

            if (selectionLength > 0)
            {
                var selectedText = text.Substring(selectionStart, selectionLength);
                var codeBlock = "```python\n" + selectedText + "\n```";
                ReplaceText(selectionStart, selectionLength, codeBlock);
                editor.SelectionStart = selectionStart + codeBlock.Length;
            }
            else
            {
                var codeBlock = "```python\n|\n```";
                text = text.Insert(selectionStart, codeBlock);
                editor.Text = text;
                editor.SelectionStart = selectionStart + 10; // Posicionar en la línea vacía
            }
            editor.Focus();
        }

        private void InsertHeading(int level)
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var selectionStart = editor.SelectionStart;
            var selectionLength = editor.SelectionLength;
            var text = editor.Text;

            var headingPrefix = new string('#', level) + " ";

            if (selectionLength > 0)
            {
                var selectedText = text.Substring(selectionStart, selectionLength);
                // Verificar si ya es encabezado
                var lineStart = text.LastIndexOf('\n', selectionStart - 1) + 1;
                var lineEnd = text.IndexOf('\n', selectionStart + selectionLength);
                if (lineEnd == -1) lineEnd = text.Length;
                var line = text.Substring(lineStart, lineEnd - lineStart);

                if (line.TrimStart().StartsWith("#"))
                {
                    // Ya es encabezado, solo cambiar el nivel
                    var match = Regex.Match(line, @"^(\s*)(#+)\s+(.*)$");
                    if (match.Success)
                    {
                        var newLine = match.Groups[1].Value + headingPrefix + match.Groups[3].Value;
                        ReplaceText(lineStart, line.Length, newLine);
                        return;
                    }
                }

                ReplaceText(selectionStart, selectionLength, headingPrefix + selectedText);
            }
            else
            {
                // Insertar en nueva línea si no está al inicio
                if (selectionStart > 0 && text[selectionStart - 1] != '\n')
                {
                    text = text.Insert(selectionStart, "\n" + headingPrefix);
                    editor.SelectionStart = selectionStart + headingPrefix.Length + 1;
                }
                else
                {
                    text = text.Insert(selectionStart, headingPrefix);
                    editor.SelectionStart = selectionStart + headingPrefix.Length;
                }
                editor.Text = text;
            }
            editor.Focus();
        }

        private void InsertHorizontalRule()
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var selectionStart = editor.SelectionStart;
            var text = editor.Text;

            // Insertar en nueva línea
            if (selectionStart > 0 && text[selectionStart - 1] != '\n')
            {
                text = text.Insert(selectionStart, "\n---\n");
                editor.SelectionStart = selectionStart + 5;
            }
            else
            {
                text = text.Insert(selectionStart, "---\n");
                editor.SelectionStart = selectionStart + 4;
            }
            editor.Text = text;
            editor.Focus();
        }

        private void InsertTable()
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var selectionStart = editor.SelectionStart;
            var text = editor.Text;

            var table = "\n| Columna 1 | Columna 2 | Columna 3 |\n|-----------|-----------|-----------|\n| | | |\n";
            text = text.Insert(selectionStart, table);
            editor.Text = text;
            editor.SelectionStart = selectionStart + table.Length - 6; // Posicionar en primera celda
            editor.Focus();
        }

        private void InsertImage()
        {
            InsertOrWrapText("![", "]()", "alt text");
            var editor = GetCurrentEditor();
            if (editor != null)
            {
                editor.SelectionStart = editor.SelectionStart - 2; // Posicionar en URL
                editor.SelectionLength = 0;
            }
        }

        private void InsertLink()
        {
            var editor = GetCurrentEditor();
            if (editor == null) return;

            var selectionStart = editor.SelectionStart;
            var selectionLength = editor.SelectionLength;
            var text = editor.Text;

            if (selectionLength > 0)
            {
                var selectedText = text.Substring(selectionStart, selectionLength);
                // Verificar si es URL
                if (Uri.TryCreate(selectedText, UriKind.Absolute, out _))
                {
                    ReplaceText(selectionStart, selectionLength, "[|](" + selectedText + ")");
                    editor.SelectionStart = selectionStart + 1;
                }
                else
                {
                    ReplaceText(selectionStart, selectionLength, "[" + selectedText + "]()");
                    editor.SelectionStart = selectionStart + selectedText.Length + 3;
                }
            }
            else
            {
                ReplaceText(selectionStart, 0, "[texto del enlace](url)");
                editor.SelectionStart = selectionStart + 1;
                editor.SelectionLength = 16; // Seleccionar "texto del enlace"
            }
            editor.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Guardar
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (TabControlMain.SelectedItem is TabItem tab && _editors.ContainsKey(tab))
                {
                    SaveFile(tab);
                }
                e.Handled = true;
                return;
            }

            // Atajos de formato (solo si hay editor activo)
            var editor = GetCurrentEditor();
            if (editor != null && editor.IsFocused)
            {
                // Negrita
                if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    InsertOrWrapText("**", "**", "texto");
                    e.Handled = true;
                    return;
                }

                // Cursiva
                if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    InsertOrWrapText("*", "*", "texto");
                    e.Handled = true;
                    return;
                }

                // Tachado
                if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    InsertOrWrapText("~~", "~~", "texto");
                    e.Handled = true;
                    return;
                }

                // Código inline
                if (e.Key == Key.Oem3 && Keyboard.Modifiers == ModifierKeys.Control) // Backtick
                {
                    InsertOrWrapText("`", "`", "código");
                    e.Handled = true;
                    return;
                }

                // Bloque código
                if (e.Key == Key.C && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    InsertCodeBlock();
                    e.Handled = true;
                    return;
                }

                // Cita
                if (e.Key == Key.Q && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    InsertOrWrapText("> ", "");
                    e.Handled = true;
                    return;
                }

                // Encabezados H1-H6
                if (e.Key >= Key.D1 && e.Key <= Key.D6 && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    var level = e.Key - Key.D0;
                    InsertHeading(level);
                    e.Handled = true;
                    return;
                }

                // Lista viñetas
                if (e.Key == Key.L && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    InsertOrWrapText("- ", "");
                    e.Handled = true;
                    return;
                }

                // Lista numerada
                if (e.Key == Key.N && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    InsertOrWrapText("1. ", "");
                    e.Handled = true;
                    return;
                }

                // Checklist
                if (e.Key == Key.T && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    InsertOrWrapText("- [ ] ", "");
                    e.Handled = true;
                    return;
                }

                // Separador
                if (e.Key == Key.H && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    InsertHorizontalRule();
                    e.Handled = true;
                    return;
                }

                // Link
                if (e.Key == Key.K && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    InsertLink();
                    e.Handled = true;
                    return;
                }

                // Imagen
                if (e.Key == Key.I && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                {
                    InsertImage();
                    e.Handled = true;
                    return;
                }
            }

            // Cerrar tab
            if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CloseTab_Click(sender, e);
                e.Handled = true;
                return;
            }
        }

        private string ProcessMermaidBlocks(string htmlContent)
        {
            // Buscar bloques de código con clase language-mermaid y convertirlos en divs de Mermaid
            var pattern = @"<pre><code class=""language-mermaid"">(.*?)</code></pre>";
            var regex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            int index = 0;
            return regex.Replace(htmlContent, match =>
            {
                var mermaidCode = match.Groups[1].Value;
                // Escapar HTML entities
                mermaidCode = mermaidCode
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Replace("&amp;", "&")
                    .Replace("&quot;", "\"");
                
                var divId = $"mermaid-{index++}";
                return $"<div class=\"mermaid\" id=\"{divId}\">{mermaidCode}</div>";
            });
        }

        private string GetNeonHtmlTemplate(string content)
        {
            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>NEON Wiki</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            background-color: #000000;
            color: #00ffff;
            font-family: 'Consolas', 'Monaco', monospace;
            line-height: 1.6;
            padding: 20px;
            font-size: 14px;
        }}

        h1, h2, h3, h4, h5, h6 {{
            color: #00ffff;
            margin: 20px 0 10px 0;
            text-shadow: 0 0 10px #00ffff;
            font-weight: bold;
        }}

        h1 {{
            font-size: 2em;
            border-bottom: 2px solid #00ffff;
            padding-bottom: 10px;
        }}

        h2 {{
            font-size: 1.5em;
            border-bottom: 1px solid #008080;
            padding-bottom: 8px;
        }}

        h3 {{ font-size: 1.3em; }}
        h4 {{ font-size: 1.1em; }}

        p {{
            margin: 10px 0;
            color: #00cccc;
        }}

        a {{
            color: #0099ff;
            text-decoration: none;
            transition: all 0.3s;
        }}

        a:hover {{
            color: #00ffff;
            text-shadow: 0 0 8px #00ffff;
        }}

        /* Código inline */
        code:not(pre code) {{
            background-color: #001a1a;
            color: #00ff00;
            padding: 2px 6px;
            border-radius: 3px;
            border: 1px solid #004d4d;
            font-family: 'Consolas', monospace;
        }}

        /* Bloques de código base */
        pre {{
            background-color: #001a1a !important;
            border: 1px solid #00ffff;
            border-radius: 5px;
            padding: 15px;
            overflow-x: auto;
            margin: 15px 0;
            box-shadow: 0 0 10px rgba(0, 255, 255, 0.3);
        }}

        pre code {{
            background: transparent !important;
            border: none;
            padding: 0;
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            font-size: 14px;
            line-height: 1.5;
            display: block;
        }}

        /* ============================================ */
        /* BLOQUES SIN LENGUAJE (``` a secas) - NEON Theme */
        /* ============================================ */
        pre:not([class*='language-']) {{
            background-color: #001a1a !important;
        }}

        pre:not([class*='language-']) code {{
            color: #00ffff;
        }}

        pre:not([class*='language-']) .token.comment,
        pre:not([class*='language-']) .token.prolog,
        pre:not([class*='language-']) .token.doctype,
        pre:not([class*='language-']) .token.cdata {{
            color: #00ff00;
            font-style: italic;
            text-shadow: 0 0 5px rgba(0, 255, 0, 0.5);
        }}

        pre:not([class*='language-']) .token.punctuation {{
            color: #00cccc;
        }}

        pre:not([class*='language-']) .token.property,
        pre:not([class*='language-']) .token.tag,
        pre:not([class*='language-']) .token.boolean,
        pre:not([class*='language-']) .token.number,
        pre:not([class*='language-']) .token.constant,
        pre:not([class*='language-']) .token.symbol,
        pre:not([class*='language-']) .token.deleted {{
            color: #00ff88;
            text-shadow: 0 0 5px rgba(0, 255, 136, 0.5);
        }}

        pre:not([class*='language-']) .token.selector,
        pre:not([class*='language-']) .token.attr-name,
        pre:not([class*='language-']) .token.string,
        pre:not([class*='language-']) .token.char,
        pre:not([class*='language-']) .token.builtin,
        pre:not([class*='language-']) .token.inserted {{
            color: #ffff00;
            text-shadow: 0 0 5px rgba(255, 255, 0, 0.5);
        }}

        pre:not([class*='language-']) .token.operator,
        pre:not([class*='language-']) .token.entity,
        pre:not([class*='language-']) .token.url {{
            color: #00ffff;
        }}

        pre:not([class*='language-']) .token.atrule,
        pre:not([class*='language-']) .token.attr-value,
        pre:not([class*='language-']) .token.keyword {{
            color: #00ffff;
            text-shadow: 0 0 8px rgba(0, 255, 255, 0.8);
            font-weight: bold;
        }}

        pre:not([class*='language-']) .token.function,
        pre:not([class*='language-']) .token.class-name {{
            color: #00ddff;
            text-shadow: 0 0 6px rgba(0, 221, 255, 0.6);
        }}

        pre:not([class*='language-']) .token.regex,
        pre:not([class*='language-']) .token.important,
        pre:not([class*='language-']) .token.variable {{
            color: #ff00ff;
            text-shadow: 0 0 5px rgba(255, 0, 255, 0.5);
        }}

        /* ============================================ */
        /* BLOQUES CON LENGUAJE (```python, etc.) - Visual Studio 2026 Dark Theme */
        /* ============================================ */
        pre[class*='language-'] {{
            background-color: #1e1e1e !important;
        }}

        pre[class*='language-'] code {{
            color: #d4d4d4;
        }}

        code[class*='language-'] {{
            background: transparent !important;
        }}

        /* Visual Studio 2026 Dark Theme - Colores exactos */
        /* Comentarios */
        pre[class*='language-'] .token.comment,
        pre[class*='language-'] .token.prolog,
        pre[class*='language-'] .token.doctype,
        pre[class*='language-'] .token.cdata {{
            color: #6a9955;
            font-style: italic;
            text-shadow: none;
        }}

        /* Puntuación */
        pre[class*='language-'] .token.punctuation {{
            color: #d4d4d4;
        }}

        /* Números, booleanos, constantes */
        pre[class*='language-'] .token.number,
        pre[class*='language-'] .token.boolean,
        pre[class*='language-'] .token.constant,
        pre[class*='language-'] .token.symbol {{
            color: #b5cea8;
            text-shadow: none;
        }}

        /* Propiedades, tags, elementos HTML */
        pre[class*='language-'] .token.property,
        pre[class*='language-'] .token.tag,
        pre[class*='language-'] .token.deleted {{
            color: #569cd6;
            text-shadow: none;
        }}

        /* Strings y caracteres */
        pre[class*='language-'] .token.string,
        pre[class*='language-'] .token.char,
        pre[class*='language-'] .token.inserted {{
            color: #ce9178;
            text-shadow: none;
        }}

        /* Selectores CSS, atributos */
        pre[class*='language-'] .token.selector,
        pre[class*='language-'] .token.attr-name {{
            color: #92c5f7;
            text-shadow: none;
        }}

        /* Operadores */
        pre[class*='language-'] .token.operator,
        pre[class*='language-'] .token.entity,
        pre[class*='language-'] .token.url {{
            color: #d4d4d4;
        }}

        /* Keywords (if, for, class, etc.) */
        pre[class*='language-'] .token.keyword {{
            color: #569cd6;
            text-shadow: none;
            font-weight: normal;
        }}

        /* At-rules CSS */
        pre[class*='language-'] .token.atrule,
        pre[class*='language-'] .token.attr-value {{
            color: #ce9178;
            text-shadow: none;
        }}

        /* Funciones */
        pre[class*='language-'] .token.function {{
            color: #dcdcaa;
            text-shadow: none;
        }}

        /* Clases y tipos */
        pre[class*='language-'] .token.class-name {{
            color: #4ec9b0;
            text-shadow: none;
        }}

        /* Variables */
        pre[class*='language-'] .token.variable {{
            color: #9cdcfe;
            text-shadow: none;
        }}

        /* Regex */
        pre[class*='language-'] .token.regex {{
            color: #d16969;
            text-shadow: none;
        }}

        /* Importante */
        pre[class*='language-'] .token.important {{
            color: #569cd6;
            text-shadow: none;
            font-weight: bold;
        }}

        /* Built-in functions */
        pre[class*='language-'] .token.builtin {{
            color: #4ec9b0;
            text-shadow: none;
        }}

        /* CSS strings específicos */
        pre[class*='language-'].language-css .token.string,
        pre[class*='language-'].style .token.string {{
            color: #ce9178;
        }}

        /* Estilos generales */
        .token.important,
        .token.bold {{
            font-weight: bold;
        }}

        .token.italic {{
            font-style: italic;
        }}

        .token.entity {{
            cursor: help;
        }}

        /* Números de línea (opcional, si se agrega) */
        .line-numbers .line-numbers-rows {{
            border-right: 1px solid #3e3e42;
        }}

        .line-numbers-rows > span:before {{
            color: #858585;
        }}

        ul, ol {{
            margin: 10px 0 10px 30px;
            color: #00cccc;
        }}

        li {{
            margin: 5px 0;
        }}

        li::marker {{
            color: #00ffff;
        }}

        blockquote {{
            border-left: 4px solid #00ffff;
            padding-left: 15px;
            margin: 15px 0;
            color: #00aaaa;
            font-style: italic;
        }}

        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 15px 0;
        }}

        th, td {{
            border: 1px solid #00ffff;
            padding: 10px;
            text-align: left;
        }}

        th {{
            background-color: #001a1a;
            color: #00ffff;
            font-weight: bold;
        }}

        td {{
            background-color: #000a0a;
            color: #00cccc;
        }}

        hr {{
            border: none;
            border-top: 2px solid #00ffff;
            margin: 20px 0;
            box-shadow: 0 0 5px #00ffff;
        }}

        strong {{
            color: #00ffff;
            font-weight: bold;
        }}

        em {{
            color: #00dddd;
            font-style: italic;
        }}

        /* Smooth scroll */
        html {{
            scroll-behavior: smooth;
        }}

        /* Custom scrollbar */
        ::-webkit-scrollbar {{
            width: 12px;
        }}

        ::-webkit-scrollbar-track {{
            background: #000000;
        }}

        ::-webkit-scrollbar-thumb {{
            background: #00ffff;
            border-radius: 6px;
        }}

        ::-webkit-scrollbar-thumb:hover {{
            background: #00cccc;
        }}

        /* Mermaid Diagram Styles */
        .mermaid {{
            background-color: #000a0a;
            border: 1px solid #00ffff;
            border-radius: 5px;
            padding: 20px;
            margin: 20px 0;
            box-shadow: 0 0 10px rgba(0, 255, 255, 0.3);
            text-align: center;
        }}

        .mermaid svg {{
            max-width: 100%;
            height: auto;
        }}

        /* Estilos para búsqueda */
        .neon-search-highlight {{
            background-color: #ffff00;
            color: #000000;
            padding: 2px 4px;
            border-radius: 3px;
            font-weight: bold;
            box-shadow: 0 0 5px rgba(255, 255, 0, 0.5);
        }}

        .neon-search-highlight-active {{
            background-color: #00ff00;
            color: #000000;
            padding: 2px 4px;
            border-radius: 3px;
            font-weight: bold;
            box-shadow: 0 0 10px rgba(0, 255, 0, 0.8);
            animation: pulse 1s ease-in-out infinite;
        }}

        @keyframes pulse {{
            0%, 100% {{
                box-shadow: 0 0 10px rgba(0, 255, 0, 0.8);
            }}
            50% {{
                box-shadow: 0 0 20px rgba(0, 255, 0, 1);
            }}
        }}
    </style>
    <!-- Prism.js para syntax highlighting -->
    <!-- Usar tema base oscuro y luego sobrescribir con colores Visual Studio -->
    <link href='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css' rel='stylesheet' />
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-core.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/plugins/autoloader/prism-autoloader.min.js'></script>
    <script>
        // Configurar Prism autoloader para cargar lenguajes automáticamente
        window.Prism = window.Prism || {{}};
        window.Prism.plugins = window.Prism.plugins || {{}};
        window.Prism.plugins.autoloader = window.Prism.plugins.autoloader || {{}};
        window.Prism.plugins.autoloader.languages_path = 'https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/';
    </script>
    <script src='https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js'></script>
    <script>
        // Initialize Mermaid with NEON theme
        mermaid.initialize({{
            startOnLoad: true,
            theme: 'dark',
            themeVariables: {{
                primaryColor: '#001a1a',
                primaryTextColor: '#00ffff',
                primaryBorderColor: '#00ffff',
                lineColor: '#00ffff',
                secondaryColor: '#003333',
                tertiaryColor: '#000a0a',
                background: '#000000',
                mainBkgColor: '#001a1a',
                secondBkgColor: '#000a0a',
                textColor: '#00ffff',
                border1: '#00ffff',
                border2: '#00cccc',
                noteBkgColor: '#001a1a',
                noteTextColor: '#00ffff',
                noteBorderColor: '#00ffff',
                actorBorder: '#00ffff',
                actorBkg: '#001a1a',
                actorTextColor: '#00ffff',
                actorLineColor: '#00cccc',
                signalColor: '#00ffff',
                signalTextColor: '#00ffff',
                labelBoxBkgColor: '#001a1a',
                labelBoxBorderColor: '#00ffff',
                labelTextColor: '#00ffff',
                loopTextColor: '#00ffff',
                activationBorderColor: '#00ffff',
                activationBkgColor: '#003333',
                sequenceNumberColor: '#000000',
                sectionBkgColor: '#001a1a',
                altBkgColor: '#000a0a',
                clusterBkg: '#001a1a',
                clusterBorder: '#00ffff',
                defaultLinkColor: '#00ffff',
                titleColor: '#00ffff',
                edgeLabelBackground: '#001a1a',
                gridColor: '#003333',
                doneColor: '#00ff00'
            }},
            flowchart: {{
                htmlLabels: true,
                curve: 'basis'
            }},
            gantt: {{
                axisFormat: '%Y-%m-%d'
            }}
        }});

        // Process Mermaid blocks and syntax highlighting when page loads
        document.addEventListener('DOMContentLoaded', function() {{
            // Render all Mermaid diagrams (already converted to divs by server-side processing)
            mermaid.run();
            
            // Aplicar syntax highlighting con Prism.js
            // Prism autoloader cargará los lenguajes automáticamente
            if (typeof Prism !== 'undefined') {{
                Prism.highlightAll();
            }}
        }});
    </script>
</head>
<body>
{content}
</body>
</html>";
        }
    }
}