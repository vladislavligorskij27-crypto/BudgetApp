using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text.Json; 
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage; 

namespace BudgetApp
{

    public class Transaction
    {
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; } = string.Empty;

        public string DisplayAmount => IsIncome ? $"+{Amount} BYN" : $"-{Amount} BYN";
        public Microsoft.Maui.Graphics.Color DisplayColor => IsIncome ? Colors.LimeGreen : Colors.IndianRed;

        public string Icon
        {
            get
            {
                if (IsIncome) return "💰";
                return Category switch
                {
                    "Еда и продукты" => "🍔",
                    "Транспорт" => "🚗",
                    "Развлечения" => "🍿",
                    "Квартплата и связь" => "🏠",
                    "Одежда" => "👕",
                    _ => "💸"
                };
            }
        }

        public string FormattedDate => Date.ToString("dd.MM.yyyy HH:mm");
    }

    public class BudgetViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Transaction> Transactions { get; set; } = new ObservableCollection<Transaction>();

        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>
        {
            "Еда и продукты", "Транспорт", "Развлечения", "Квартплата и связь", "Одежда", "Другое"
        };

        private decimal _totalBalance;
        private decimal _totalIncome;
        private decimal _totalExpense;
        private string _inputTitle = string.Empty;
        private string _inputAmount = string.Empty;
        private string _selectedCategory = "Другое";

        public decimal TotalBalance
        {
            get => _totalBalance;
            set { _totalBalance = value; OnPropertyChanged(); }
        }

        public decimal TotalIncome
        {
            get => _totalIncome;
            set { _totalIncome = value; OnPropertyChanged(); CalculateProgress(); }
        }

        public decimal TotalExpense
        {
            get => _totalExpense;
            set { _totalExpense = value; OnPropertyChanged(); CalculateProgress(); }
        }

        private double _expenseProgress;
        public double ExpenseProgress
        {
            get => _expenseProgress;
            set { _expenseProgress = value; OnPropertyChanged(); }
        }

        public string InputTitle
        {
            get => _inputTitle;
            set { _inputTitle = value; OnPropertyChanged(); }
        }

        public string InputAmount
        {
            get => _inputAmount;
            set { _inputAmount = value; OnPropertyChanged(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public ICommand AddIncomeCommand { get; }
        public ICommand AddExpenseCommand { get; }
        public ICommand RemoveTransactionCommand { get; }

        public ICommand ClearAllCommand { get; }

        public BudgetViewModel()
        {
            AddIncomeCommand = new Command(AddIncome);
            AddExpenseCommand = new Command(AddExpense);
            RemoveTransactionCommand = new Command<Transaction>(RemoveTransaction);
            ClearAllCommand = new Command(ClearAll);

            LoadData();
        }

        private void AddIncome() => ProcessTransaction(isIncome: true);
        private void AddExpense() => ProcessTransaction(isIncome: false);

        private void ProcessTransaction(bool isIncome)
        {
            if (string.IsNullOrWhiteSpace(InputTitle))
            {
                Application.Current.MainPage.DisplayAlert("Ошибка", "Пожалуйста, введите название операции!", "ОК");
                return;
            }

            if (string.IsNullOrWhiteSpace(InputAmount))
            {
                Application.Current.MainPage.DisplayAlert("Ошибка", "Пожалуйста, введите сумму!", "ОК");
                return;
            }

            string safeAmount = InputAmount.Replace(',', '.');

            if (decimal.TryParse(safeAmount, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedAmount))
            {
                var newTransaction = new Transaction
                {
                    Title = InputTitle,
                    Amount = parsedAmount,
                    IsIncome = isIncome,
                    Date = DateTime.Now,
                    Category = isIncome ? "Доход" : SelectedCategory
                };

                Transactions.Insert(0, newTransaction);

                RecalculateTotals(); 
                SaveData();          

                InputTitle = string.Empty;
                InputAmount = string.Empty;
            }
            else
            {
                Application.Current.MainPage.DisplayAlert("Ошибка", "Сумма введена неверно. Используйте только цифры!", "ОК");
            }
        }

        private void RemoveTransaction(Transaction transactionToОDelete)
        {
            if (transactionToОDelete == null) return;

            Transactions.Remove(transactionToОDelete);
            RecalculateTotals();
            SaveData(); 
        }


        private async void ClearAll()
        {

            bool answer = await Application.Current.MainPage.DisplayAlert("Очистка", "Вы точно хотите удалить всю историю? Это действие нельзя отменить.", "Да, удалить", "Отмена");

            if (answer == true)
            {
                Transactions.Clear(); 
                RecalculateTotals();
                SaveData(); 
            }
        }

        private void RecalculateTotals()
        {
            TotalIncome = 0;
            TotalExpense = 0;
            TotalBalance = 0;

            foreach (var t in Transactions)
            {
                if (t.IsIncome)
                {
                    TotalIncome += t.Amount;
                    TotalBalance += t.Amount;
                }
                else
                {
                    TotalExpense += t.Amount;
                    TotalBalance -= t.Amount;
                }
            }
            CalculateProgress();
        }

        private void CalculateProgress()
        {
            if (TotalIncome == 0) ExpenseProgress = 0;
            else
            {
                double ratio = (double)(TotalExpense / TotalIncome);
                ExpenseProgress = ratio > 1 ? 1 : ratio;
            }
        }


        private void SaveData()
        {
            string jsonText = JsonSerializer.Serialize(Transactions);

            Preferences.Default.Set("MyBudgetHistory", jsonText);
        }

        private void LoadData()
        {
            string jsonText = Preferences.Default.Get("MyBudgetHistory", string.Empty);

            if (!string.IsNullOrEmpty(jsonText))
            {
                var loadedTransactions = JsonSerializer.Deserialize<ObservableCollection<Transaction>>(jsonText);

                if (loadedTransactions != null)
                {
                    Transactions = loadedTransactions;
                    RecalculateTotals(); 
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}