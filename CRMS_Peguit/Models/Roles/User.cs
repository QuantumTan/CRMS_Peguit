using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CRMS_Peguit.winforms.Models.Roles
{
    public enum UserRole
    {
        SuperAdmin,
        Admin,
        Manager,
        SalesStaff
    }

    
    // PARENT CLASS - User (inherits BaseEntity)
    
    public abstract class User : BaseEntity
    {
        // PUBLIC - safe to read/display everywhere
        public string FullName;
        public string Email;

        // PRIVATE - hidden (Encapsulation)
        private string _passwordHash = string.Empty;
        private UserRole _role;

        protected User(string fullName, string email, UserRole role) : base()
        {
            FullName = fullName;
            Email = email;
            _role = role;
        }

        // PROPERTIES - controlled access
        public UserRole Role
        {
            get { return _role; }
        }

        // Only exposed through a method, never a public setter.
        // Only an Admin-driven action should call this in practice.
        public void ChangeRole(UserRole newRole)
        {
            _role = newRole;
            UpdateTimestamp();
        }

        // Password is never exposed directly - hashed on the way in
        public void SetPassword(string plainTextPassword)
        {
            _passwordHash = CRMS_Peguit.infrastructure.Security.PasswordHasher.Hash(plainTextPassword);
            UpdateTimestamp();
        }

        public bool VerifyPassword(string plainTextPassword)
        {
            return CRMS_Peguit.infrastructure.Security.PasswordHasher.Verify(plainTextPassword, _passwordHash);
        }

        // ABSTRACT - each role defines its own accessible modules/dashboard
        public abstract List<string> GetAccessibleModules();
        public abstract string GetDashboardType();
    }

}
