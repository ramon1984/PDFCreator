﻿using pdfforge.Obsidian;
using pdfforge.PDFCreator.Conversion.Settings.Enums;
using pdfforge.PDFCreator.UI.Presentation.Helper.Translation;
using pdfforge.PDFCreator.UI.Presentation.UserControls.Profiles.TabHelper;
using System.Collections.Generic;

namespace pdfforge.PDFCreator.UI.Presentation.UserControls.Profiles.Tabs.ConvertTabs
{
    public class ConvertTabViewModel : ProfileUserControlViewModel<ConvertTabTranslation>, ITabViewModel
    {
        public ConvertTabViewModel(ITranslationUpdater updater, ISelectedProfileProvider selectedProfile) : base(updater, selectedProfile)
        {
            SetOutputFormatCommand = new DelegateCommand<OutputFormat>(SetOutputFormatExecute);
            CurrentProfileChanged += (sender, args) => RaisePropertyChanged(nameof(OutputFormat));
        }

        // Required for Icons Styles
        public OutputFormat OutputFormat => CurrentProfile?.OutputFormat ?? OutputFormat.Pdf;

        public IEnumerable<OutputFormat> OutputFormats => System.Enum.GetValues(typeof(OutputFormat)) as OutputFormat[];

        public string Title => Translation.ConvertTab;
        public IconList Icon => IconList.ConversionSettings;
        public bool HiddenByGPO => false;
        public bool BlockedByGPO => false;

        public DelegateCommand<OutputFormat> SetOutputFormatCommand { get; }

        private void SetOutputFormatExecute(OutputFormat parameter)
        {
            CurrentProfile.OutputFormat = parameter;
            RaisePropertyChanged(nameof(OutputFormat));
        }
    }
}
