using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MiddleClickToPeekDefinition
{
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [ComVisible(true)]
    public class FreshOptionsPage : DialogPage
    {
        private CommandSetting ctrlMiddleClickSetting = CommandSetting.PeekDefinition;
        private CommandSetting middleClickSetting = CommandSetting.GoToDefinition;

        [Category("Middleclick Actions")]
        [DisplayName("Middleclick")]
        [Description("Controls which action is called for Middleclick")]
        [TypeConverter(typeof(EnumTypeConverter))]
        public CommandSetting MiddleClickSetting
        {
            get { return middleClickSetting; }
            set { middleClickSetting = value; }
        }

        [Category("Middleclick Actions")]
        [DisplayName("Ctrl-Middleclick")]
        [Description("Controls which action is called for Ctrl-Middleclick")]
        [TypeConverter(typeof(EnumTypeConverter))]
        public CommandSetting CtrlMiddleClickSetting
        {
            get { return ctrlMiddleClickSetting; }
            set { ctrlMiddleClickSetting = value; }
        }
    }
}
