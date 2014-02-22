using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiddleClickToPeekDefinition
{
    #region Key Section

    [Export(typeof(IKeyProcessorProvider))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("code")]
    [Name("MiddleClickToPeekDefinition")]
    [Order(Before = "VisualStudioKeyboardProcessor")]
    internal sealed class KeyboardProcessorFactory : IKeyProcessorProvider
    {
        public KeyProcessor GetAssociatedProcessor(IWpfTextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(GoToDefKeyProcessor),
            () => new GoToDefKeyProcessor(CtrlKeyState.GetStateForView(view)));
        }
    }

    internal sealed class CtrlKeyState
    {
        internal static CtrlKeyState GetStateForView(ITextView view)
        {
            return view.Properties.GetOrCreateSingletonProperty(typeof(CtrlKeyState), () => new CtrlKeyState());
        }

        bool _enabled = false;

        internal bool Enabled
        {
            get
            {
                bool ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                                (Keyboard.Modifiers & ModifierKeys.Shift) == 0;
                if (ctrlDown != _enabled)
                    Enabled = ctrlDown;

                return _enabled;
            }

            set
            {
                bool oldVal = _enabled;
                _enabled = value;
                if (oldVal != _enabled)
                {
                    var temp = CtrlKeyStateChanged;
                    if (temp != null)
                        temp(this, new EventArgs());
                }
            }
        }

        internal event EventHandler<EventArgs> CtrlKeyStateChanged;
    }

    internal sealed class GoToDefKeyProcessor : KeyProcessor
    {
        CtrlKeyState _state;

        public GoToDefKeyProcessor(CtrlKeyState state)
        {
            _state = state;
        }

        void UpdateState(KeyEventArgs args)
        {
            _state.Enabled = (args.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0 &&
                             (args.KeyboardDevice.Modifiers & ModifierKeys.Shift) == 0;
        }

        public override void PreviewKeyDown(KeyEventArgs args)
        {
            UpdateState(args);
        }

        public override void PreviewKeyUp(KeyEventArgs args)
        {
            UpdateState(args);
        }
    }


    #endregion

    #region Mouse Section

    [Export(typeof(IMouseProcessorProvider))]
    [ContentType("code")]
    [Name("MiddleClickToPeekDefinition")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
    [Order(Before = "WordSelection")]
    internal sealed class MouseProcessorFactory : IMouseProcessorProvider
    {
        [Import]
        IClassifierAggregatorService _aggregatorFactory = null;

        [Import]
        ITextStructureNavigatorSelectorService _navigatorService = null;

        [Import]
        SVsServiceProvider _globalServiceProvider = null;

        public IMouseProcessor GetAssociatedProcessor(IWpfTextView view)
        {
            var buffer = view.TextBuffer;

            IOleCommandTarget shellCommandDispatcher = GetShellCommandDispatcher(view);

            if (shellCommandDispatcher == null)
                return null;

            return new GoToDefMouseHandler(view,
                                           shellCommandDispatcher,
                                           _aggregatorFactory.GetClassifier(buffer),
                                           _navigatorService.GetTextStructureNavigator(buffer),
                                           CtrlKeyState.GetStateForView(view));
        }

        IOleCommandTarget GetShellCommandDispatcher(ITextView view)
        {
            return _globalServiceProvider.GetService(typeof(SUIHostCommandDispatcher)) as IOleCommandTarget;
        }

    }

    internal sealed class GoToDefMouseHandler : MouseProcessorBase
    {
        readonly IWpfTextView _view;
        readonly IClassifier _aggregator;
        readonly ITextStructureNavigator _navigator;
        readonly IOleCommandTarget _commandTarget;
        CtrlKeyState _state;

        public GoToDefMouseHandler(IWpfTextView view, IOleCommandTarget commandTarget, IClassifier aggregator,
                                   ITextStructureNavigator navigator, CtrlKeyState state)
        {
            _view = view;
            _commandTarget = commandTarget;
            _aggregator = aggregator;
            _navigator = navigator;
            _state = state;            
        }

        Point? _mouseDownAnchorPoint;

        private bool InDragOperation(Point anchorPoint, Point currentPoint)
        {
            return Math.Abs(anchorPoint.X - currentPoint.X) >= SystemParameters.MinimumHorizontalDragDistance &&
                   Math.Abs(anchorPoint.Y - currentPoint.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }

        public override void PreprocessMouseLeave(MouseEventArgs e)
        {
            _mouseDownAnchorPoint = null;
        }

        public override void PreprocessMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton.ToString().Equals("Middle") && _mouseDownAnchorPoint.HasValue)
            {
                var currentMousePosition = RelativeToView(e.GetPosition(_view.VisualElement));

                if (!InDragOperation(_mouseDownAnchorPoint.Value, currentMousePosition))
                {
                    if (IsSignificantElement(RelativeToView(e.GetPosition(_view.VisualElement))))
                    {
                        if (_state.Enabled) //ctrl down
                            this.DispatchGoToDef();
                        else                //ctrl up
                            this.DispatchPeekDef();                            
                    }

                    e.Handled = true;
                }
            }

            _mouseDownAnchorPoint = null;
        }

        public override void PreprocessMouseDown(MouseButtonEventArgs e)
        {
            MouseButton button = e.ChangedButton;

            var position = RelativeToView(e.GetPosition(_view.VisualElement));
            var line = _view.TextViewLines.GetTextViewLineContainingYCoordinate(position.Y);
            if (line == null)
                return;
            _view.Caret.MoveTo(line, position.X);

            if (button.ToString().Equals("Middle"))
            {
                _mouseDownAnchorPoint = RelativeToView(e.GetPosition(_view.VisualElement));
                IsSignificantElement(RelativeToView(e.GetPosition(_view.VisualElement)));
            }
        }

        Point RelativeToView(Point position)
        {
            return new Point(position.X + _view.ViewportLeft, position.Y + _view.ViewportTop);
        }

        bool IsSignificantElement(Point position)
        {
            try
            {
                var line = _view.TextViewLines.GetTextViewLineContainingYCoordinate(position.Y);
                if (line == null)
                    return false;

                var bufferPosition = line.GetBufferPositionFromXCoordinate(position.X);

                if (!bufferPosition.HasValue)
                    return false;

                var extent = _navigator.GetExtentOfWord(bufferPosition.Value);
                if (!extent.IsSignificant)
                    return false;

                if (_view.TextBuffer.ContentType.IsOfType("csharp"))
                {
                    string lineText = bufferPosition.Value.GetContainingLine().GetText().Trim();
                    if (lineText.StartsWith("using", StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                foreach (var classification in _aggregator.GetClassificationSpans(extent.Span))
                {
                    var name = classification.ClassificationType.Classification.ToLower();
                    if (name.Contains("identifier") || name.Contains("user types") ||
                        (name.Contains("keyword") && IsAppropriateKeyword(classification.Span.GetText())
                        && _view.TextBuffer.ContentType.IsOfType("csharp")))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private readonly string[] _keywords = new[] { "base", "this", "float", "double", "string", "int", "long", "object" };

        private bool IsAppropriateKeyword(string keyword)
        {
            for (int i = 0; i < _keywords.Length; i++)
                if (_keywords[i] == keyword)
                    return true;

            return false;
        }

        void DispatchGoToDef()
        {
            try
            {
                Guid cmdGroup = VSConstants.GUID_VSStandardCommandSet97;
                int hr = _commandTarget.Exec(ref cmdGroup,
                                             (uint)VSConstants.VSStd97CmdID.GotoDefn,
                                             (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                                             IntPtr.Zero,
                                             IntPtr.Zero);
            }
            catch (Exception e)
            { 
            }
        }

        void DispatchPeekDef()
        {
            try
            {
                Guid cmdGroup = VSConstants.CMDSETID.StandardCommandSet12_guid;
                int hr = _commandTarget.Exec(ref cmdGroup,
                                             (uint)VSConstants.VSStd12CmdID.PeekDefinition,
                                             (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                                             IntPtr.Zero,
                                             IntPtr.Zero);
            }
            catch (Exception e)
            { 
            }
        }

    }

    #endregion



}
