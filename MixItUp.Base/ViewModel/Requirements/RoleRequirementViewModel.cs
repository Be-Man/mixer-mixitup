﻿using MixItUp.Base.Model.Requirements;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Requirements
{
    public class UserRoleViewModel : UIViewModelBase, IComparable<UserRoleViewModel>
    {
        public OldUserRoleEnum Role
        {
            get { return this.role; }
            set
            {
                this.role = value;
                this.NotifyPropertyChanged();
            }
        }
        private OldUserRoleEnum role;

        public ICommand DeleteAdvancedRoleCommand { get; private set; }

        private RoleRequirementViewModel viewModel;

        public UserRoleViewModel(RoleRequirementViewModel viewModel, OldUserRoleEnum role)
        {
            this.viewModel = viewModel;
            this.Role = role;

            this.DeleteAdvancedRoleCommand = this.CreateCommand(() =>
            {
                this.viewModel.SelectedAdvancedRoles.Remove(this);
            });
        }

        public string Name { get { return EnumLocalizationHelper.GetLocalizedName(this.Role); } }

        public int CompareTo(UserRoleViewModel other) { return this.Role.CompareTo(other.Role); }
    }

    public class RoleRequirementViewModel : RequirementViewModelBase
    {
        private static PatreonBenefit NonePatreonBenefit = new PatreonBenefit() { ID = string.Empty, Title = "None" };

        public bool IsAdvancedRolesSelected
        {
            get { return this.isAdvancedRolesSelected; }
            set
            {
                this.isAdvancedRolesSelected = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("ShowSimpleRoles");
                this.NotifyPropertyChanged("IsSubscriberRole");
            }
        }
        private bool isAdvancedRolesSelected = false;

        public bool ShowSimpleRoles { get { return !this.IsAdvancedRolesSelected; } }

        public IEnumerable<OldUserRoleEnum> Roles { get { return UserV2Model.GetSelectableUserRoles(); } }

        public OldUserRoleEnum SelectedRole
        {
            get { return this.selectedRole; }
            set
            {
                this.selectedRole = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("IsSubscriberRole");
            }
        }
        private OldUserRoleEnum selectedRole = OldUserRoleEnum.User;

        public IEnumerable<OldUserRoleEnum> AdvancedRoles
        {
            get
            {
                List<OldUserRoleEnum> roles = new List<OldUserRoleEnum>(UserV2Model.GetSelectableUserRoles());
                roles.Remove(OldUserRoleEnum.VIPExclusive);
                return roles;
            }
        }

        public OldUserRoleEnum SelectedAdvancedRole
        {
            get { return this.selectedAdvancedRole; }
            set
            {
                this.selectedAdvancedRole = value;
                this.NotifyPropertyChanged();
            }
        }
        private OldUserRoleEnum selectedAdvancedRole = OldUserRoleEnum.User;

        public SortableObservableCollection<UserRoleViewModel> SelectedAdvancedRoles { get; set; } = new SortableObservableCollection<UserRoleViewModel>();

        public bool IsSubscriberRole
        {
            get
            {
                if (this.IsAdvancedRolesSelected)
                {
                    return this.SelectedAdvancedRoles.Any(r => r.Role == OldUserRoleEnum.Subscriber);
                }
                else
                {
                    return this.SelectedRole == OldUserRoleEnum.Subscriber;
                }
            }
        }

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

        public bool IsPatreonConnected { get { return ServiceManager.Get<PatreonService>().IsConnected; } }

        public IEnumerable<PatreonBenefit> PatreonBenefits
        {
            get
            {
                List<PatreonBenefit> benefits = new List<PatreonBenefit>();
                benefits.Add(RoleRequirementViewModel.NonePatreonBenefit);
                if (this.IsPatreonConnected)
                {
                    benefits.AddRange(ServiceManager.Get<PatreonService>().Campaign.Benefits.Values.OrderBy(b => b.Title));
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

        public ICommand AddAdvancedRoleCommand { get; set; }

        public RoleRequirementViewModel()
        {
            this.AddAdvancedRoleCommand = this.CreateCommand(() =>
            {
                if (!this.SelectedAdvancedRoles.Any(r => r.Role == this.SelectedAdvancedRole))
                {
                    this.SelectedAdvancedRoles.Add(new UserRoleViewModel(this, this.SelectedAdvancedRole));
                }
                this.NotifyPropertyChanged("IsSubscriberRole");
            });
        }

        public RoleRequirementViewModel(RoleRequirementModel requirement)
            : this()
        {
            if (requirement.RoleList.Count > 0)
            {
                this.IsAdvancedRolesSelected = true;
                foreach (OldUserRoleEnum role in requirement.RoleList)
                {
                    this.SelectedAdvancedRoles.Add(new UserRoleViewModel(this, role));
                }
            }
            else
            {
                this.SelectedRole = requirement.Role;
            }
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

        public override async Task<Result> Validate()
        {
            if (this.IsAdvancedRolesSelected)
            {
                if (this.SelectedAdvancedRoles.Count == 0)
                {
                    return new Result(MixItUp.Base.Resources.RoleRequirementAtLeastOneRoleMustBeSelected);
                }
            }
            return await base.Validate();
        }

        public override RequirementModelBase GetRequirement()
        {
            if (this.IsAdvancedRolesSelected)
            {
                return new RoleRequirementModel(this.SelectedAdvancedRoles.Select(r => r.Role), this.SubscriberTier, this.selectedPatreonBenefit?.ID);
            }
            else
            {
                return new RoleRequirementModel(this.SelectedRole, this.SubscriberTier, this.selectedPatreonBenefit?.ID);
            }
        }
    }
}
