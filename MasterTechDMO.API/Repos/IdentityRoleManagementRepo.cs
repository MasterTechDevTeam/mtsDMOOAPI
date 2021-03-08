﻿using MasterTechDMO.API.Areas.Identity.Data;
using MasterTechDMO.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using mtsDMO.Context.RoleManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasterTechDMO.API.Repos
{
    public class IdentityRoleManagementRepo : IIdentityRoleManagementRepo
    {
        private readonly IServiceProvider _serviceProvider;
        private MTDMOContext _context;

        public IdentityRoleManagementRepo(IServiceProvider serviceProvider, MTDMOContext context)
        {
            _serviceProvider = serviceProvider;
            _context = context;
        }

        public async Task<KeyValuePair<bool, string>> CreateRolesAsync(Guid orgId, string roleName)
        {
            try
            {
                var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                //var userManager = _serviceProvider.GetRequiredService<UserManager<DMOUsers>>();

                var roleCheck = await roleManager.RoleExistsAsync(roleName);

                if (!roleCheck)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (MapOrgWithRole(orgId, roleName))
                    {
                        return new KeyValuePair<bool, string>(true, $"{roleName} created sucessfully");
                    }
                    else
                        return new KeyValuePair<bool, string>(false, $"Oops! Something went wrong while assigning {roleName} role to organization");
                }
                return new KeyValuePair<bool, string>(true, $"{roleName} role already exists.");

            }
            catch (Exception Ex)
            {
                return new KeyValuePair<bool, string>(false, Ex.Message);
            }
        }

        public async Task<APICallResponse<bool>> AssignRoleToUserAsync(string userId, string roleName)
        {
            var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = _serviceProvider.GetRequiredService<UserManager<DMOUsers>>();

            bool isRoleExists = await roleManager.RoleExistsAsync(roleName);
            if (!isRoleExists)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            DMOUsers user = await userManager.FindByIdAsync(userId);

            if (user != null)
            {
                var result = await userManager.AddToRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                    return new APICallResponse<bool>()
                    {
                        IsSuccess = true,
                        Message = new List<string>() { string.Format($"{roleName} assigned to {userId} user") },
                        Respose = true
                    };
                }
                else
                {
                    List<string> iError = result.Errors.Select(x => x.Description).ToList();

                    return new APICallResponse<bool>
                    {
                        IsSuccess = false,
                        Message = iError,
                        Status = "IdentityError",
                        Respose = false
                    };
                }
            }

            return new APICallResponse<bool>
            {
                IsSuccess = false,
                Message = new List<string>() { "Can not assign role.User not found." },
                Status = "Warning",
                Respose = false
            };
        }

        public async Task<APICallResponse<List<DMOOrgRoles>>> GetRolesAsync(Guid orgId)
        {
            try
            {
                var lstOrgRoles = new APICallResponse<List<DMOOrgRoles>>();
                if (orgId != null && IsOrgUser(orgId))
                {
                    lstOrgRoles.Respose = _context.DMOOrgRoles.Where(x => x.OrgId == orgId).ToList();
                    lstOrgRoles.Message = new List<string> { $"{lstOrgRoles.Respose.Count} roles founds." };
                    lstOrgRoles.Status = "Success";
                }
                else if (orgId != null && IsMTAdmin(orgId))
                {
                    lstOrgRoles.Respose = _context.DMOOrgRoles.ToList();
                    lstOrgRoles.Message = new List<string> { $"{lstOrgRoles.Respose.Count} roles founds." };
                    lstOrgRoles.Status = "Success";
                }
                else
                {
                    lstOrgRoles.Respose = null;
                    lstOrgRoles.Message = new List<string> { "Either user not found or user is missing permission." };
                    lstOrgRoles.Status = "Warning";
                }

                lstOrgRoles.IsSuccess = true;
                return lstOrgRoles;
            }
            catch (Exception Ex)
            {
                return new APICallResponse<List<DMOOrgRoles>>
                {
                    IsSuccess = false,
                    Message = new List<string>() { Ex.Message },
                    Status = "Error",
                    Respose = null
                };
            }
        }

        public async Task<APICallResponse<bool>> CheckUserInRole(Guid Id)
        {
            try
            {
                var callResponse = new APICallResponse<bool>();

                var userInRole = _context.UserRoles.Where(x => x.UserId == Id.ToString()).FirstOrDefault();
                if (userInRole != null)
                {
                    var roleDetails = _context.Roles.Where(x => x.Id == userInRole.RoleId).FirstOrDefault();
                    if (roleDetails.Name == Constants.BaseRole.MTAdmin || roleDetails.Name == Constants.BaseRole.Org)
                    {
                        callResponse.Respose = true;
                        callResponse.Message = new List<string> { $"user {Id} found with {roleDetails.Name} role" };
                        callResponse.Status = "Success";
                    }
                }
                else
                {
                    callResponse.Respose = false;
                    callResponse.Message = new List<string> { "User not found." };
                    callResponse.Status = "Warning";
                }
                callResponse.IsSuccess = true;
                return callResponse;
            }
            catch (Exception Ex)
            {
                return new APICallResponse<bool>
                {
                    IsSuccess = false,
                    Message = new List<string>() { Ex.Message },
                    Status = "Error",
                    Respose = false
                };
            }
        }

        public async Task<KeyValuePair<bool, string>> CreateBaseRoleAsync(string roleName)
        {
            try
            {
                var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                var roleCheck = await roleManager.RoleExistsAsync(roleName);

                if (!roleCheck)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    return new KeyValuePair<bool, string>(true, $"{roleName} created sucessfully");
                }
                return new KeyValuePair<bool, string>(true, $"{roleName} role already exists.");

            }
            catch (Exception Ex)
            {
                return new KeyValuePair<bool, string>(false, Ex.Message);
            }
        }

        #region Helper Methods
        private bool MapOrgWithRole(Guid orgId, string roleName)
        {
            try
            {
                var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                var roleId = roleManager.FindByNameAsync(roleName).Result.Id;

                _context.DMOOrgRoles.Add(new DMOOrgRoles
                {
                    DisplayName = roleName,
                    Id = Guid.NewGuid(),
                    OrgId = orgId,
                    RoleId = Guid.Parse(roleId)
                });

                _context.SaveChanges();
                return true;
            }
            catch (Exception Ex)
            {
                return false;
            }
        }

        private bool IsOrgUser(Guid orgId)
        {
            try
            {
                var orgUserData = _context.Users.Where(x => x.Id == orgId.ToString() && x.IsOrg == true).FirstOrDefault();
                if (orgUserData != null)
                {
                    var userInRole = _context.UserRoles.Where(x => x.UserId == orgId.ToString()).FirstOrDefault();
                    if (userInRole != null)
                    {
                        var roleDetails = _context.Roles.Where(x => x.Id == userInRole.RoleId).FirstOrDefault();
                        if (roleDetails.Name == Constants.BaseRole.Org)
                        {
                            return true;
                        }
                        return false;
                    }
                    return false;
                }
                else
                    return false;
            }
            catch (Exception Ex)
            {
                return false;
            }
        }

        private bool IsMTAdmin(Guid userId)
        {
            try
            {
                var userData = _context.Users.Where(x => x.Id == userId.ToString()).FirstOrDefault();
                if (userData != null)
                {
                    var userInRole = _context.UserRoles.Where(x => x.UserId == userId.ToString()).FirstOrDefault();
                    if (userInRole != null)
                    {
                        var roleDetails = _context.Roles.Where(x => x.Id == userInRole.RoleId).FirstOrDefault();
                        if (roleDetails.Name == Constants.BaseRole.MTAdmin)
                        {
                            return true;
                        }
                        return false;
                    }
                    return false;
                }
                else
                    return false;
            }
            catch (Exception Ex)
            {
                return false;
            }
        }
        #endregion
    }
}
