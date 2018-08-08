using System;
using System.Runtime.Serialization;


namespace ConsoleExample
{
    public class UserDto :  BaseDto
    {
        [DataMember]
        public string UserName { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string OldPassword { get; set; }

        [DataMember]
        public DateTime? DtLastLogin { get; set; }

        [DataMember]
        public int? TotalLogin { get; set; }

        [DataMember]
        public byte? PasswordTry { get; set; }

        [DataMember]
        public string ActivationCode { get; set; }

        [DataMember]
        public string Theme { get; set; }

        [DataMember]
        public bool IsUserActive { get; set; }

        //public override string ToDescription()
        //{
        //    return this.UserName;
        //}

        //public override T Copy<T>()
        //{
        //    UserDto _new = new UserDto();
        //    _new.UserName = this.UserName;
        //    _new.Password = this.Password;
        //    _new.OldPassword = this.OldPassword;
        //    _new.DtLastLogin = this.DtLastLogin;
        //    _new.TotalLogin = this.TotalLogin;
        //    _new.PasswordTry = this.PasswordTry;
        //    _new.ActivationCode = this.ActivationCode;
        //    _new.Theme = this.Theme;
        //    _new.IsUserActive = this.IsUserActive;
        //    return BaseCopy<T>(_new);
        //}

        public override string ToString()
        {
            return this.UserName;
        }

        public override bool Equals(Object obj)
        {
            UserDto userDto = obj as UserDto;
            if (userDto == null)
                return false;
            else
                return UserName.Equals(userDto.UserName);
        }

        public override int GetHashCode()
        {
            return UserName.GetHashCode();
        }

    }
}
