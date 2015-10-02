using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace MiddleClickToPeekDefinition
{
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [ComVisible(true)]
    public class OptionsPage : DialogPage
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

    public class EnumTypeConverter : EnumConverter
    {
        public EnumTypeConverter() : base(typeof(CommandSetting)) { }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            string str = value as string;
            if (str != null)
            {
                if (str == "Nothing") return CommandSetting.Nothing;
                if (str == "Peek Definition") return CommandSetting.PeekDefinition;
                if (str == "Go To Definition") return CommandSetting.GoToDefinition;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                string result = null;
                if ((int)value == 0) result = "Nothing";
                else if ((int)value == 1) result = "Peek Definition";
                else if ((int)value == 2) result = "Go To Definition";

                if (result != null) return result;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
