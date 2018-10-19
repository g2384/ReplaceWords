using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;

namespace ReplaceWords
{
    public class MainWindowViewModel : BindableBase
    {
        public const string SettingFile = "settings.json";

        public Settings Settings { get; set; }

        public MainWindowViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingFile))
            {
                var setting = File.ReadAllText(SettingFile);
                Settings = JsonConvert.DeserializeObject<Settings>(setting);
            }
            else
            {
                Settings = new Settings();
                Settings.Init();
            }
        }

        private DelegateCommand _startCommand;

        public DelegateCommand StartCommand =>
            _startCommand ?? (_startCommand = new DelegateCommand(() => RunAsyncTask(ReplaceAsync), () => _isStartButtonEnabled && !string.IsNullOrWhiteSpace(SourceFilePath)));

        private DelegateCommand _analyzeCommand;

        public DelegateCommand AnalyzeCommand =>
            _analyzeCommand ?? (_analyzeCommand = new DelegateCommand(() => RunAsyncTask(AnalyzeAsync), () => _isAnalyzeButtonEnabled && !string.IsNullOrWhiteSpace(SourceFilePath)));

        private string _sourceFilePath;

        public string SourceFilePath
        {
            get => _sourceFilePath ?? (_sourceFilePath = Settings.SourceFilePath);
            set
            {
                if (SetProperty(ref _sourceFilePath, value))
                {
                    StartCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _searchTerm;

        public string SearchTerm
        {
            get => _searchTerm ?? (_searchTerm = Settings.SearchTerm);
            set => SetProperty(ref _searchTerm, value);
        }

        private string _replacementTerm;

        public string ReplacementTerm
        {
            get => _replacementTerm ?? (_replacementTerm = Settings.RepalcementTerm);
            set => SetProperty(ref _replacementTerm, value);
        }

        private bool? _isRegex;

        public bool IsRegex
        {
            get => (_isRegex ?? (_isRegex = Settings.IsRegex)).Value;
            set => SetProperty(ref _isRegex, value);
        }

        private string _delimiter;

        public string Delimiter
        {
            get => _delimiter ?? (_delimiter = Settings.Delimiter);
            set => SetProperty(ref _delimiter, value);
        }

        private bool? _showFileDetails;

        public bool ShowFileDetails
        {
            get => (_showFileDetails ?? (_showFileDetails = Settings.ShowFileDetails)).Value;
            set
            {
                if (SetProperty(ref _showFileDetails, value))
                {
                    RunAsyncTask(DisplayWords);
                }
            }
        }

        private string _status;

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private double _progress;

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private string _fileExtensions;

        public string FileExtensions
        {
            get => _fileExtensions ?? (_fileExtensions = string.Join("; ", Settings.FileExtensions));
            set => SetProperty(ref _fileExtensions, value);
        }

        private string _excludeFolders;

        public string ExcludeFolders
        {
            get => _excludeFolders ?? (_excludeFolders = "\"" + string.Join("\"; \"", Settings.ExcludeFolders) + "\"");
            set => SetProperty(ref _excludeFolders, value);
        }

        private bool _isStartButtonEnabled = true;

        private bool _isAnalyzeButtonEnabled = true;

        private bool _isProgressVisible = false;

        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set => SetProperty(ref _isProgressVisible, value);
        }

        private string _unknownWordsStat;

        public string UnknownWordsStat
        {
            get => _unknownWordsStat;
            set => SetProperty(ref _unknownWordsStat, value);
        }

        private ObservableCollection<Rule> _rules;

        public ObservableCollection<Rule> Rules
        {
            get => _rules;
            set => SetProperty(ref _rules, value);
        }

        private void ChangeStartCommandCanExecute(bool isStartButtonEnabled)
        {
            var dispatcher = GetDispatcher();
            dispatcher?.Invoke(() =>
            {
                _isStartButtonEnabled = isStartButtonEnabled;
                StartCommand.RaiseCanExecuteChanged();
            }, DispatcherPriority.Send);
        }

        private void ChangeAnalyzeCommandCanExecute(bool isStartButtonEnabled)
        {
            var dispatcher = GetDispatcher();
            dispatcher?.Invoke(() =>
            {
                _isStartButtonEnabled = isStartButtonEnabled;
                StartCommand.RaiseCanExecuteChanged();
            }, DispatcherPriority.Send);
        }

        private static Dispatcher GetDispatcher()
        {
            var app = Application.Current;
            return app?.Dispatcher;
        }

        private int _totalFilesCount;

        private async void RunAsyncTask(Action action)
        {
            await Task.Run(action);
        }

        private void AnalyzeAsync()
        {
            try
            {
                ChangeAnalyzeCommandCanExecute(false);
                IsProgressVisible = true;
                var sp = new Stopwatch();
                sp.Start();
                Analyze();
                sp.Stop();
                IsProgressVisible = false;
                var timeElapsed = $"Time elapsed: {sp.Elapsed.Hours}h {sp.Elapsed.Minutes}m {sp.Elapsed.Seconds}s {sp.Elapsed.Milliseconds}ms";
                Status = $"Completed ({timeElapsed}, Analysed {_totalFilesCount} files)";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
                Status = "Error occurred";
            }
            finally
            {
                ChangeAnalyzeCommandCanExecute(true);
            }
        }

        private void ReplaceAsync()
        {
            try
            {
                ChangeStartCommandCanExecute(false);
                IsProgressVisible = true;
                var sp = new Stopwatch();
                sp.Start();
                ReplaceWords();
                sp.Stop();
                IsProgressVisible = false;
                var timeElapsed = $"Time elapsed: {sp.Elapsed.Hours}h {sp.Elapsed.Minutes}m {sp.Elapsed.Seconds}s {sp.Elapsed.Milliseconds}ms";
                Status = $"Completed ({timeElapsed}, processed {_totalFilesCount} files, replaced {_totalRepalced} occurences)";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
                Status = "Error occurred";
            }
            finally
            {
                ChangeStartCommandCanExecute(true);
            }
        }

        private class ReplacementExamples
        {
            public ReplacementExamples()
            {
                _examples = new ConcurrentDictionary<string, object>();
                _uniqueExamples = new ConcurrentDictionary<string, object>();
            }

            private const int RequiredExamples = 5;

            public int Count { get; set; }

            public int UniqueCount { get; set; }

            public void AddExample(string example)
            {
                Count++;
                if (Count < RequiredExamples && !_examples.ContainsKey(example))
                {
                    _examples[example] = null;
                }
            }

            public void AddUniqueExample(string example)
            {
                UniqueCount++;
                if (UniqueCount < RequiredExamples && !_uniqueExamples.ContainsKey(example))
                {
                    _uniqueExamples[example] = null;
                }
            }

            private readonly ConcurrentDictionary<string, object> _examples;
            private readonly ConcurrentDictionary<string, object> _uniqueExamples;

            public string GetExamples()
            {
                var examples = default(string);
                if (UniqueCount > 0)
                {
                    examples = string.Join(Environment.NewLine, _examples.Keys.Select(e => "[unique] " + e.Trim()));
                }

                if (UniqueCount >= RequiredExamples)
                {
                    return examples;
                }

                var extraExamples = string.Join(Environment.NewLine,
                    _examples.Keys.Select(e => e.Trim()).Take(RequiredExamples - UniqueCount));
                if (UniqueCount > 0)
                {
                    return examples + Environment.NewLine + extraExamples;
                }

                return extraExamples;
            }
        }

        private IDictionary<SearchReplacementRegexPair, ReplacementExamples> _rawRules;

        private class SearchReplacementRegexPair
        {
            public SearchReplacementRegexPair(SearchReplacementStringPair rule)
            {
                SearchTerm = new Regex(rule.SearchTerm);
                ReplacementTerm = rule.ReplacementTerm;
            }

            public Regex SearchTerm { get; set; }
            public string ReplacementTerm { get; set; }
        }

        private class SearchReplacementStringPair
        {
            public SearchReplacementStringPair(string searchTerm, string replacementTerm)
            {
                SearchTerm = searchTerm;
                ReplacementTerm = replacementTerm;
            }

            public string SearchTerm { get; set; }
            public string ReplacementTerm { get; set; }
        }

        private bool PrepareEnvironment(string status, out string path)
        {
            Progress = 0;
            _totalFilesCount = 0;
            UnknownWordsStat = "";
            Status = status;
            path = SourceFilePath;
            if (!Directory.Exists(SourceFilePath))
            {
                MessageBox.Show($"Wrong directory \"{SourceFilePath}\"", "Error", MessageBoxButton.OK);
                return false;
            }

            SaveSettings();

            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            InitialiseRules();

            return true;
        }

        private void Analyze()
        {
            if (!PrepareEnvironment("Analyzing...", out var path))
            {
                return;
            }

            var allFiles = GetAllFiles(path);

            _totalFilesCount = allFiles.Count;
            var processedFiles = 0;
            Parallel.ForEach(allFiles, file =>
            {
                var shortFilePath = file.Replace(Settings.SourceFilePath, "");
                CheckLine(shortFilePath);
                var lines = File.ReadAllLines(file);
                Parallel.ForEach(lines, line =>
                {
                    try
                    {
                        CheckLine(line);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace, "Error", MessageBoxButton.OK);
                    }
                });

                processedFiles++;
                Status = processedFiles + "/" + _totalFilesCount;
                Progress = (double)processedFiles * 100 / _totalFilesCount;
            });

            lock (_rawRules)
            {
                DisplayWords();
            }
        }

        private int _totalRepalced;

        private void InitialiseRules()
        {
            _rawRules = new Dictionary<SearchReplacementRegexPair, ReplacementExamples>();
            var searchTerm = SearchTerm;
            var replacementTerm = ReplacementTerm;
            var rules = IsRegex
                ? new[] { new SearchReplacementStringPair(searchTerm, replacementTerm) }
                : GetRegexRules(searchTerm, replacementTerm);

            foreach (var rule in rules)
            {
                _rawRules.Add(new SearchReplacementRegexPair(rule), new ReplacementExamples());
            }
        }

        private List<string> GetAllFiles(string path)
        {
            var allFiles = new List<string>();
            foreach (var ext in Settings.FileExtensions)
            {
                var extension = ext.StartsWith("*") ? ext : "*" + ext;
                allFiles.AddRange(FileHelper.GetAllFiles(path, extension, info =>
                    Settings.FileExtensions.Any(i => i == ".*" || Path.GetExtension(info.Name) == i)
                    && !info.IsReadOnly
                    && Settings.ExcludeFolders.TrueForAll(
                        i => (info.Directory?.FullName.Replace(path, @"\") + "\\").Contains(i) == false
                    )));
            }

            return allFiles;
        }

        private IEnumerable<SearchReplacementStringPair> GetRegexRules(string searchTerm, string replacementTerm)
        {
            ConvertStrings(searchTerm, out var upperInitial, out var firstLowerInitial, out var spacedUpperInitial, out var spacedFirstLowerInitial);
            ConvertStrings(replacementTerm, out var rUpperInitial, out var rFirstLowerInitial, out var rSpacedUpperInitial, out var rSpacedFirstLowerInitial);
            yield return new SearchReplacementStringPair($@"(?<!\w){upperInitial}(?!\w)", rUpperInitial);
            yield return new SearchReplacementStringPair($@"(?<![A-Z]){upperInitial}(?![a-z])", rUpperInitial);
            yield return new SearchReplacementStringPair($@"(?<![a-zA-Z]){firstLowerInitial}(?![a-z])", rFirstLowerInitial);
            yield return new SearchReplacementStringPair($@"(?<!\w){spacedUpperInitial}(?!\w)", rSpacedUpperInitial);
            yield return new SearchReplacementStringPair($@"(?<!\w){spacedFirstLowerInitial}(?!\w)", rSpacedFirstLowerInitial);
        }

        private void ConvertStrings(string searchTerm, out string upperInitial, out string firstLowerInitial, out string spacedUpperInitial, out string spacedFirstLowerInitial)
        {
            var singleWords = searchTerm.Split(Delimiter).ToArray();
            var allUpperInitials = singleWords.Select(e => e.ToUpperInitial()).ToArray();
            upperInitial = string.Join("", allUpperInitials); // e.g. AppleBanana
            var firstLowerInitials = new[] { singleWords.First().ToLowerInitial() }.Concat(singleWords.Skip(1).Select(e => e.ToUpperInitial()));
            firstLowerInitial = string.Join("", firstLowerInitials); // e.g. appleBanana
            spacedUpperInitial = string.Join(" ", allUpperInitials); // e.g. Apple Banana
            spacedFirstLowerInitial = string.Join(" ", singleWords.Select(e => e.ToLowerInitial())); // e.g. apple banana
        }

        private void ReplaceWords()
        {
            if (!PrepareEnvironment("Starting...", out var path))
            {
                return;
            }

            var allFiles = GetAllFiles(path);

            _totalFilesCount = allFiles.Count;
            var processedFiles = 0;
            var count = new ConcurrentDictionary<string, int>();
            Parallel.ForEach(allFiles, file =>
            {
                var c = 0;
                var text = File.ReadAllText(file);
                var newText = text;
                foreach (var rule in _rawRules) {
                    newText = Regex.Replace(newText, rule.Key.SearchTerm.ToString(), m =>
                    {
                        c++;
                        return rule.Key.ReplacementTerm;
                    });
                }

                if (!string.Equals(text, newText, StringComparison.Ordinal))
                {
                    File.WriteAllText(file, newText);
                }

                var newFilePath = file;
                foreach (var rule in _rawRules)
                {
                    newFilePath = Regex.Replace(newFilePath, rule.Key.SearchTerm.ToString(), m => {
                        c++;
                        return m.Result(rule.Key.ReplacementTerm);
                    });
                }
                if (file != newFilePath)
                {
                    File.Move(file, newFilePath);
                }

                processedFiles++;
                Status = processedFiles + "/" + _totalFilesCount;
                Progress = (double)processedFiles * 100 / _totalFilesCount;
                count[file] = c;
            });
            _totalRepalced = count.Values.Sum();
        }

        private void CheckLine(string line)
        {
            var matchedCount = 0;
            var matchedRule = default(ReplacementExamples);
            foreach (var rule in _rawRules)
            {
                if (rule.Key.SearchTerm.IsMatch(line))
                {
                    matchedCount++;
                    matchedRule = rule.Value;
                    rule.Value.AddExample(line);
                }
            }

            if (matchedCount == 1)
            {
                matchedRule?.AddUniqueExample(line);
            }
        }

        private void SaveSettings()
        {
            Settings.SourceFilePath = SourceFilePath;
            Settings.ShowFileDetails = ShowFileDetails;
            Settings.SearchTerm = SearchTerm;
            Settings.RepalcementTerm = ReplacementTerm;
            Settings.IsRegex = IsRegex;
            Settings.Delimiter = Delimiter;
            if (!string.IsNullOrWhiteSpace(ExcludeFolders))
            {
                var extensions = ExcludeFolders.Substring(1, ExcludeFolders.Length - 2);
                Settings.ExcludeFolders = new Regex(@""";\s+""").Split(extensions).Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(FileExtensions))
            {
                Settings.FileExtensions = new Regex(@"[^\*\.\w]+").Split(FileExtensions).Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }

            File.WriteAllText(SettingFile, JsonConvert.SerializeObject(Settings, Formatting.Indented));
        }

        private void DisplayWords()
        {
            if (_rawRules == null)
            {
                return;
            }

            Status = "Preparing results for displaying";

            var rules = new ObservableCollection<Rule>();
            foreach (var rule in _rawRules)
            {
                rules.Add(new Rule(rule.Key.SearchTerm.ToString(), rule.Key.ReplacementTerm, rule.Value.GetExamples(), rule.Value.Count, rule.Value.UniqueCount));
            }

            Rules = rules;
            Status = "Completed";
        }
    }
}
