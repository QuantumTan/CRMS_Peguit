using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NEXA.Model
{
    public enum UserRole
    {
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
        private string _passwordHash;
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
            _passwordHash = HashPassword(plainTextPassword);
            UpdateTimestamp();
        }

        public bool VerifyPassword(string plainTextPassword)
        {
            return _passwordHash == HashPassword(plainTextPassword);
        }

        private string HashPassword(string plainText)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                return Convert.ToBase64String(bytes);
            }
        }

        // ABSTRACT - each role defines its own accessible modules/dashboard
        public abstract List<string> GetAccessibleModules();
        public abstract string GetDashboardType();
    }

}