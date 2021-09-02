﻿using MixItUp.API.Models;
using MixItUp.Base;
using MixItUp.Base.Model;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;

namespace MixItUp.WPF.Services.DeveloperAPI
{
    [RoutePrefix("api/users")]
    public class UserController : ApiController
    {
        public static async Task<UserV2Model> GetUserData(string usernameOrID)
        {
            UserV2Model user = null;
            if (!string.IsNullOrEmpty(usernameOrID))
            {
                if (Guid.TryParse(usernameOrID, out Guid userId))
                {
                    user = await ServiceManager.Get<UserService>().GetUserDataByID(userId);
                }
                else if (int.TryParse(usernameOrID, out int twitchID))
                {
                    user = await ServiceManager.Get<UserService>().GetUserDataByPlatformID(StreamingPlatformTypeEnum.Twitch, usernameOrID);
                }
                else
                {
                    user = await ServiceManager.Get<UserService>().GetUserDataByPlatformUsername(StreamingPlatformTypeEnum.Twitch, usernameOrID);
                }
            }
            return user;
        }

        [Route]
        [HttpPost]
        public async Task<IEnumerable<User>> BulkGet([FromBody] IEnumerable<string> usernamesOrIDs)
        {
            List<User> users = new List<User>();
            foreach (var usernameOrID in usernamesOrIDs)
            {
                UserV2Model user = await UserController.GetUserData(usernameOrID);
                if (user != null)
                {
                    users.Add(UserFromUserDataViewModel(user));
                }
            }

            return users;
        }

        [Route("{usernameOrID}")]
        [HttpGet]
        public async Task<User> Get(string usernameOrID)
        {
            UserV2Model user = await UserController.GetUserData(usernameOrID);
            if (user == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find user: {usernameOrID}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "User not found"
                };
                throw new HttpResponseException(resp);
            }

            return UserFromUserDataViewModel(user);
        }

        [Route("twitch/{usernameOrID}")]
        [HttpGet]
        public async Task<User> GetTwitch(string usernameOrID)
        {
            UserV2Model user = null;
            if (!string.IsNullOrEmpty(usernameOrID))
            {
                user = await ServiceManager.Get<UserService>().GetUserDataByPlatformID(StreamingPlatformTypeEnum.Twitch, usernameOrID);
                if (user == null)
                {
                    user = await ServiceManager.Get<UserService>().GetUserDataByPlatformUsername(StreamingPlatformTypeEnum.Twitch, usernameOrID);
                }
            }

            if (user == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find user: {usernameOrID}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "User not found"
                };
                throw new HttpResponseException(resp);
            }

            return UserFromUserDataViewModel(user);
        }

        [Route("{usernameOrID}")]
        [HttpPut, HttpPatch]
        public async Task<User> Update(string usernameOrID, [FromBody] User updatedUserData)
        {
            UserV2Model user = await UserController.GetUserData(usernameOrID);
            if (user == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find user: {usernameOrID}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "User not found"
                };
                throw new HttpResponseException(resp);
            }

            return UpdateUser(user, updatedUserData);
        }

        private User UpdateUser(UserV2Model user, User updatedUserData)
        {
            if (updatedUserData == null || !updatedUserData.ID.Equals(user.ID))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to parse update data from POST body." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid POST Body"
                };
                throw new HttpResponseException(resp);
            }

            if (updatedUserData.ViewingMinutes.HasValue)
            {
                user.ViewingMinutes = updatedUserData.ViewingMinutes.Value;
            }

            foreach (CurrencyAmount currencyData in updatedUserData.CurrencyAmounts)
            {
                if (ChannelSession.Settings.Currency.ContainsKey(currencyData.ID))
                {
                    ChannelSession.Settings.Currency[currencyData.ID].SetAmount(user, currencyData.Amount);
                }
            }

            return UserFromUserDataViewModel(user);
        }

        [Route("{usernameOrID}/currency/{currencyID:guid}/adjust")]
        [HttpPut, HttpPatch]
        public async Task<User> AdjustUserCurrency(string usernameOrID, Guid currencyID, [FromBody] AdjustCurrency currencyUpdate)
        {
            UserV2Model user = await UserController.GetUserData(usernameOrID);
            if (user == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find user: {usernameOrID}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "User not found"
                };
                throw new HttpResponseException(resp);
            }

            return AdjustCurrency(user, currencyID, currencyUpdate);
        }

        [Route("{usernameOrID}/inventory/{inventoryID:guid}/adjust")]
        [HttpPut, HttpPatch]
        public async Task<User> AdjustUserInventory(string usernameOrID, Guid inventoryID, [FromBody]AdjustInventory inventoryUpdate)
        {
            UserV2Model user = await UserController.GetUserData(usernameOrID);
            if (user == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find user: {usernameOrID}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "User not found"
                };
                throw new HttpResponseException(resp);
            }
            return AdjustInventory(user, inventoryID, inventoryUpdate);
        }

        [Route("top")]
        [HttpGet]
        public IEnumerable<User> Get(int count = 10)
        {
            if (count < 1)
            {
                // TODO: Consider checking or a max # too? (100?)
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Count must be greater than 0." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Count too low"
                };
                throw new HttpResponseException(resp);
            }

            Dictionary<Guid, UserV2Model> allUsersDictionary = ChannelSession.Settings.Users.ToDictionary();

            IEnumerable<UserV2Model> allUsers = allUsersDictionary.Select(kvp => kvp.Value);
            allUsers = allUsers.Where(u => !u.IsCurrencyRankExempt);

            List<User> userList = new List<User>();
            foreach (UserV2Model user in allUsers.OrderByDescending(u => u.ViewingMinutes).Take(count))
            {
                userList.Add(UserFromUserDataViewModel(user));
            }
            return userList;
        }

        public static User UserFromUserDataViewModel(UserV2Model userData)
        {
            User user = new User
            {
                ID = userData.ID,
                TwitchID = userData.TwitchID,
                Username = userData.Username,
                ViewingMinutes = userData.ViewingMinutes
            };

            foreach (CurrencyModel currency in ChannelSession.Settings.Currency.Values)
            {
                user.CurrencyAmounts.Add(CurrencyController.CurrencyAmountFromUserCurrencyViewModel(currency, currency.GetAmount(userData)));
            }

            foreach (InventoryModel inventory in ChannelSession.Settings.Inventory.Values)
            {
                user.InventoryAmounts.Add(InventoryController.InventoryAmountFromUserInventoryViewModel(inventory, inventory.GetAmounts(userData)));
            }

            return user;
        }

        private User AdjustCurrency(UserV2Model user, Guid currencyID, [FromBody] AdjustCurrency currencyUpdate)
        {
            if (!ChannelSession.Settings.Currency.ContainsKey(currencyID))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find currency: {currencyID.ToString()}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Currency ID not found"
                };
                throw new HttpResponseException(resp);
            }

            if (currencyUpdate == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to parse currency adjustment from POST body." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid POST Body"
                };
                throw new HttpResponseException(resp);
            }

            CurrencyModel currency = ChannelSession.Settings.Currency[currencyID];

            if (currencyUpdate.Amount < 0)
            {
                int quantityToRemove = currencyUpdate.Amount * -1;
                if (!currency.HasAmount(user, quantityToRemove))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                    {
                        Content = new ObjectContent<Error>(new Error { Message = "User does not have enough currency to remove" }, new JsonMediaTypeFormatter(), "application/json"),
                        ReasonPhrase = "Not Enough Currency"
                    };
                    throw new HttpResponseException(resp);
                }

                currency.SubtractAmount(user, quantityToRemove);
            }
            else if (currencyUpdate.Amount > 0)
            {
                currency.AddAmount(user, currencyUpdate.Amount);
            }

            return UserFromUserDataViewModel(user);
        }

        private User AdjustInventory(UserV2Model user, Guid inventoryID, [FromBody]AdjustInventory inventoryUpdate)
        {
            if (!ChannelSession.Settings.Inventory.ContainsKey(inventoryID))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new ObjectContent<Error>(new Error { Message = $"Unable to find inventory: {inventoryID.ToString()}." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Inventory ID not found"
                };
                throw new HttpResponseException(resp);
            }

            if (inventoryUpdate == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to parse inventory adjustment from POST body." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid POST Body"
                };
                throw new HttpResponseException(resp);
            }

            InventoryModel inventory = ChannelSession.Settings.Inventory[inventoryID];

            if (string.IsNullOrEmpty(inventoryUpdate.Name) || !inventory.ItemExists(inventoryUpdate.Name))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new ObjectContent<Error>(new Error { Message = "Unable to find requested inventory item." }, new JsonMediaTypeFormatter(), "application/json"),
                    ReasonPhrase = "Invalid Inventory Item"
                };
                throw new HttpResponseException(resp);
            }

            InventoryItemModel item = inventory.GetItem(inventoryUpdate.Name);
            if (inventoryUpdate.Amount < 0)
            {
                int quantityToRemove = inventoryUpdate.Amount * -1;
                if (!inventory.HasAmount(user, item, quantityToRemove))
                {
                    // If the request is to remove inventory, but user doesn't have enough, fail
                    var resp = new HttpResponseMessage(HttpStatusCode.Forbidden)
                    {
                        Content = new ObjectContent<Error>(new Error { Message = "User does not have enough inventory to remove" }, new JsonMediaTypeFormatter(), "application/json"),
                        ReasonPhrase = "Not Enough Inventory"
                    };
                    throw new HttpResponseException(resp);
                }

                inventory.SubtractAmount(user, item, quantityToRemove);
            }
            else if (inventoryUpdate.Amount > 0)
            {
                inventory.AddAmount(user, item, inventoryUpdate.Amount);
            }

            return UserFromUserDataViewModel(user);
        }
    }
}
