using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TranslateWPFProject
{
    public enum EnInputLang {
        RUSSIAN,
        ENGLISH
    }

    public partial class MainWindow : Window
    {
        private const String RUS_INPUT_LANG = "Russian";
        private const String ENG_INPUT_LANG = "English";
        private readonly String STATUS_TRANSLATED = "Status: Translated";
        private readonly String STATUS_NOT_TRANSLATED = "Status: Not Translated";

        private readonly String API_KEY = "trnsl.1.1.20180902T164113Z.8e6c4bba6692950f.cbd511b3c5997d74fe4809d5bdbcda1f57e5149f";
        private readonly String CONTENT_TYPE_HEADER = "application/x-www-form-urlencoded";
        private const String EN_LANG_DIR = "en";
        private const String RU_LANG_DIR = "ru";


        private EnInputLang m_currentInputLang = EnInputLang.RUSSIAN;
        private static readonly System.Net.Http.HttpClient HttpClient;

        public MainWindow()
        {
            InitializeComponent();

            if (!InternetConnectionAvailable())
            {
                MessageBox.Show("Приложению требуется наличие интернет-соединения. Установите интернет-соединение и попробуйте еще раз.");
                Application.Current.Shutdown();
            }

            this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("MoonUICore.xaml", UriKind.RelativeOrAbsolute) });
        }

        static MainWindow()
        {
            HttpClient = new System.Net.Http.HttpClient();
        }

        private void SetInputLang(String langDir)
        {
            if (langDir == RU_LANG_DIR) ChangeLanguageInLangLabels(EnInputLang.RUSSIAN);
            else ChangeLanguageInLangLabels(EnInputLang.ENGLISH);
        }

        private void ChangeLanguageButton_Click(object sender, RoutedEventArgs e)
        {
            if (m_currentInputLang == EnInputLang.RUSSIAN) ChangeLanguageInLangLabels(EnInputLang.ENGLISH);
            else ChangeLanguageInLangLabels(EnInputLang.RUSSIAN);
            SwapTextBoxesContent();
        }

        private void ChangeLanguageInLangLabels(EnInputLang targetLang)
        {
            if (targetLang == EnInputLang.RUSSIAN)
            {
                InputLangLabel.Content = RUS_INPUT_LANG;
                OutputLangLabel.Content = ENG_INPUT_LANG;
                m_currentInputLang = EnInputLang.RUSSIAN;
            }
            else
            {
                InputLangLabel.Content = ENG_INPUT_LANG;
                OutputLangLabel.Content = RUS_INPUT_LANG;
                m_currentInputLang = EnInputLang.ENGLISH;
            }
        }

        private void SwapTextBoxesContent()
        {
            String backupStr = OutputTextBox.Text;
            OutputTextBox.Text = InputTextBox.Text;
            InputTextBox.Text = backupStr;

            UpdateCountSymbolsOutputText();
            SetStatus();
        }


        private bool InternetConnectionAvailable()
        {
            try
            {
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create("https://www.google.com");
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse) request.GetResponse();

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    response.Close();
                    return true;
                }
                else
                {
                    response.Close();
                    return false;
                }
            }
            catch (System.Net.WebException)
            {
                return false;
            }
        }

        private async void TranslateText()
        {
            if (InputTextBox.Text == String.Empty)
            {
                MessageBox.Show("Input text is empty.");
                return;
            }

            try
            {
                TranslateButton.Content = new ProgressBar() { IsIndeterminate = true }; ;

                using (System.Net.Http.StringContent httpContent = new System.Net.Http.StringContent($"text={InputTextBox.Text}", Encoding.UTF8, CONTENT_TYPE_HEADER))
                {
                    System.Net.Http.HttpResponseMessage responseTypeLang = await HttpClient.PostAsync(
                        $"https://translate.yandex.net/api/v1.5/tr.json/detect?hint={EN_LANG_DIR},{RU_LANG_DIR}&key={API_KEY}", httpContent
                    );
                    responseTypeLang.EnsureSuccessStatusCode();
                    String responseTypeLangBody = await responseTypeLang.Content.ReadAsStringAsync();

                    Dictionary<String, String> responseTypeLangDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<String, String>>(responseTypeLangBody);
                    String codeTypeLangValue = null;
                    responseTypeLangDict.TryGetValue("code", out codeTypeLangValue);


                    if (codeTypeLangValue == "200")
                    {
                        String langValue = null;
                        responseTypeLangDict.TryGetValue("lang", out langValue);

                        SetInputLang(langValue);
                    }
                    else throw new Exception();
                }

                using (System.Net.Http.StringContent httpContent = new System.Net.Http.StringContent($"text={InputTextBox.Text}", Encoding.UTF8, CONTENT_TYPE_HEADER))
                {
                    String langDirection = m_currentInputLang == EnInputLang.RUSSIAN ? "ru-en" : "en-ru"; // en-ru -> in-out
                    System.Net.Http.HttpResponseMessage responseTranslatedText = await HttpClient.PostAsync(
                        $"https://translate.yandex.net/api/v1.5/tr.json/translate?lang={langDirection}&key={API_KEY}&format=plain", httpContent
                    );
                    responseTranslatedText.EnsureSuccessStatusCode();
                    String responseTranslatedTextBody = await responseTranslatedText.Content.ReadAsStringAsync();

                    Dictionary<String, Object> responseTranslatedTextDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<String, Object>>(responseTranslatedTextBody);
                    Object codeTranslatedTextValue = null;
                    responseTranslatedTextDict.TryGetValue("code", out codeTranslatedTextValue);

                    if ( (Int64) codeTranslatedTextValue == 200)
                    {
                        var textArray = (Newtonsoft.Json.Linq.JArray) responseTranslatedTextDict["text"];
                        OutputTextBox.Text = textArray.First.ToString();
                    }
                    else throw new Exception();
                }

            }
            catch (System.Net.Http.HttpRequestException e)
            {
                MessageBox.Show($"Error Message: {e.Message}");
                return;
            }
            catch (Exception e) {
                MessageBox.Show($"Error Message: {e.Message}");
                return;
            }
            finally
            {
                TranslateButton.Content = "Translate";
            }

            UpdateCountSymbolsOutputText();
            SetStatus();
        }

        private void UpdateCountSymbolsInputText()
        {
            CountSymbolsInputTextLabel.Content = InputTextBox.Text.Length.ToString();
        }

        private void UpdateCountSymbolsOutputText()
        {
            CountSymbolsOutputTextLabel.Content = OutputTextBox.Text.Length.ToString();
        }

        private void SetStatus()
        {
            StatusTranslatedLabel.Content = STATUS_TRANSLATED;
            StatusTranslatedLabel.Foreground = Brushes.Green;
        }

        private void ResetStatus()
        {
            StatusTranslatedLabel.Content = STATUS_NOT_TRANSLATED;
            StatusTranslatedLabel.Foreground = Brushes.Red;
        }

        private void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            TranslateText();
            // ...
        }

        private void TranslateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            TranslateButton_Click(this, e);
        }

        private void SwapLanguagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ChangeLanguageButton_Click(this, e);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StatusTranslatedLabel == null) return;
            ResetStatus();
            UpdateCountSymbolsInputText();

            ClearInputMenuItem.IsEnabled = true;
            if (InputTextBox.Text.Length == 0) CopyOutputMenuItem.IsEnabled = false;
            else CopyOutputMenuItem.IsEnabled = true;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            UpdateCountSymbolsInputText();
        }

        private void CopyOutputMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutputTextBox.Text);
        }

        private void ClearInputMenuItem_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = String.Empty;
            OutputTextBox.Text = String.Empty;

            CountSymbolsInputTextLabel.Content = "0";
            CountSymbolsOutputTextLabel.Content = "0";

            ClearInputMenuItem.IsEnabled = false;
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = System.IO.Directory.GetCurrentDirectory()
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InputTextBox.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
