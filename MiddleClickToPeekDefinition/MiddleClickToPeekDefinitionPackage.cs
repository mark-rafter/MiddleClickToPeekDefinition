using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiddleClickToPeekDefinition
{
    [ProvideBindingPath]        
    [PackageRegistration(UseManagedResourcesOnly=true)]
    [ProvideOptionPage(typeof(OptionsPage), "MiddleClickDefinition", "General", 0, 0, true)]    
    public class MiddleClickToPeekDefinitionPackage : Package
    {       
    }
}
