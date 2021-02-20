﻿using MixItUp.Base.Model.Requirements;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services.External;
using System.Collections.Generic;
using System.Linq;

namespace MixItUp.Base.ViewModel.Requirements
{
    public class RoleRequirementViewModel : RequirementViewModelBase
    {
        private static PatreonBenefit NonePatreonBenefit = new PatreonBenefit() { ID = string.Empty, Title = "None" };

        public IEnumerable<UserRoleEnum> Roles { get { return UserDataModel.GetSelectableUserRoles(); } }

        public UserRoleEnum SelectedRole
        {
            get { return this.selectedRole; }
            set
            {
                this.selectedRole = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("IsSubscriberRole");
            }
        }
        private UserRoleEnum selectedRole = UserRoleEnum.User;

        public bool IsSubscriberRole { get { return this.SelectedRole == UserRoleEnum.Subscriber; } }

        public IEnumerable<int> SubscriberTiers { get { return new List<int>() { 1, 2, 3 }; } }
        public int SubscriberTier
        {
            get { return this.subscriberTier; }
            set
            {
                this.subscriberTier = value;
                this.NotifyPropertyChanged();
            }
        }
        private int subscriberTier = 1;

        public bool IsPatreonConnected { get { return ChannelSession.Services.Patreon.IsConnected; } }

        public IEnumerable<PatreonBenefit> PatreonBenefits
        {
            get
            {
                List<PatreonBenefit> benefits = new List<PatreonBenefit>();
                benefits.Add(RoleRequirementViewModel.NonePatreonBenefit);
                if (this.IsPatreonConnected)
                {
                    benefits.AddRange(ChannelSession.Services.Patreon.Campaign.Benefits.Values.OrderBy(b => b.Title));
                }
                return benefits;
            }
        }

        public PatreonBenefit SelectedPatreonBenefit
        {
            get { return this.selectedPatreonBenefit; }
            set
            {
                this.selectedPatreonBenefit = value;
                this.NotifyPropertyChanged();
            }
        }
        private PatreonBenefit selectedPatreonBenefit = RoleRequirementViewModel.NonePatreonBenefit;

        public RoleRequirementViewModel() { }

        public RoleRequirementViewModel(RoleRequirementModel requirement)
        {
            this.SelectedRole = requirement.Role;
            this.SubscriberTier = requirement.SubscriberTier;
            if (this.IsPatreonConnected && !string.IsNullOrEmpty(requirement.PatreonBenefitID))
            {
                this.SelectedPatreonBenefit = this.PatreonBenefits.FirstOrDefault(b => b.ID.Equals(requirement.PatreonBenefitID));
                if (this.SelectedPatreonBenefit == null)
                {
                    this.SelectedPatreonBenefit = RoleRequirementViewModel.NonePatreonBenefit;
                }
            }
        }

        public override RequirementModelBase GetRequirement()
        {
            return new RoleRequirementModel(this.SelectedRole, this.SubscriberTier, this.selectedPatreonBenefit?.ID);
        }
    }
}
