using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.PageModels.AuthFlow
{
    public partial class CodeInputModel : ObservableObject
    {
        private string _username;
        private string _password;

        [ObservableProperty]
        private string code;

        public void SetData(string username, string password)
        {
            _username = username;
            _password = password;
        }
    }
}
