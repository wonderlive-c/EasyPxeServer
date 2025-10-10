// ------------------------------------------------------------------------
// This file is licensed to you under the MIT License.
// ------------------------------------------------------------------------

using Microsoft.FluentUI.AspNetCore.Components;

namespace FluentPxeServer.Components;

public partial class SiteSettings
{
    private IDialogReference? _dialog;

    private async Task OpenSiteSettingsAsync()
    {
        logger.LogInformation($"Open site settings");
        _dialog = await DialogService.ShowPanelAsync<SiteSettingsPanel>(new DialogParameters()
        {
            ShowTitle       = true,
            Title           = "Site settings",
            Alignment       = HorizontalAlignment.Right,
            PrimaryAction   = "OK",
            SecondaryAction = null,
            ShowDismiss     = true
        });

        await _dialog.Result;
    }
}