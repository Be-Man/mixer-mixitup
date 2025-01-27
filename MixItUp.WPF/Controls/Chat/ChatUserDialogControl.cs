﻿using MixItUp.Base;
using MixItUp.Base.Model.User;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using MixItUp.WPF.Controls.Dialogs;
using MixItUp.WPF.Windows.Users;
using StreamingClient.Base.Util;
using System.Threading.Tasks;

namespace MixItUp.WPF.Controls.Chat
{
    public static class ChatUserDialogControl
    {
        public static async Task ShowUserDialog(UserViewModel user)
        {
            if (user != null && !user.IsAnonymous)
            {
                object result = await DialogHelper.ShowCustom(new UserDialogControl(user));
                if (result != null)
                {
                    UserDialogResult dialogResult = EnumHelper.GetEnumValueFromString<UserDialogResult>(result.ToString());
                    switch (dialogResult)
                    {
                        case UserDialogResult.Purge:
                            await ChannelSession.Services.Chat.PurgeUser(user);
                            break;
                        case UserDialogResult.Timeout1:
                            await ChannelSession.Services.Chat.TimeoutUser(user, 60);
                            break;
                        case UserDialogResult.Timeout5:
                            await ChannelSession.Services.Chat.TimeoutUser(user, 300);
                            break;
                        case UserDialogResult.Ban:
                            if (await DialogHelper.ShowConfirmation(string.Format(Resources.BanUserPrompt, user.FullDisplayName)))
                            {
                                await ChannelSession.Services.Chat.BanUser(user);
                            }
                            break;
                        case UserDialogResult.Unban:
                            await ChannelSession.Services.Chat.UnbanUser(user);
                            break;
                        case UserDialogResult.PromoteToMod:
                            if (await DialogHelper.ShowConfirmation(string.Format(Resources.PromoteUserPrompt, user.FullDisplayName)))
                            {
                                await ChannelSession.Services.Chat.ModUser(user);
                            }
                            break;
                        case UserDialogResult.DemoteFromMod:
                            if (await DialogHelper.ShowConfirmation(string.Format(Resources.DemoteUserPrompt, user.FullDisplayName)))
                            {
                                await ChannelSession.Services.Chat.UnmodUser(user);
                            }
                            break;
                        case UserDialogResult.ChannelPage:
                            ProcessHelper.LaunchLink(user.ChannelLink);
                            break;
                        case UserDialogResult.EditUser:
                            UserDataEditorWindow window = new UserDataEditorWindow(user.Data);
                            await Task.Delay(100);
                            window.Show();
                            await Task.Delay(100);
                            window.Focus();
                            break;
                        case UserDialogResult.Close:
                        default:
                            // Just close
                            break;
                    }
                }
            }
        }
    }
}
