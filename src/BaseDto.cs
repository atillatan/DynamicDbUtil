using System;
using System.Runtime.Serialization;

namespace ConsoleExample
{
    [Serializable]
    [DataContract]
    public abstract class BaseDto :  IComparable
    {
        public BaseDto()
        {
            IsActive = true;
        }

        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public DateTime CreateDate { get; set; }
        [DataMember]
        public int CreatedBy { get; set; }
        [DataMember]
        public DateTime? UpdateDate { get; set; }
        [DataMember]
        public int? UpdatedBy { get; set; }
        [DataMember]
        public bool IsActive { get; set; }

        public void OnDeserialization(Object o) { }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
        public virtual int CompareTo(object obj)
        {
            if (obj == null) return 1;

            BaseDto baseDto = obj as BaseDto;
            if (baseDto != null)
                return this.Id.CompareTo(baseDto.Id);
            else
                throw new ArgumentException("Object is not a BaseDto");
        }
        public override bool Equals(Object obj)
        {
            BaseDto baseDto = obj as BaseDto;
            if (baseDto == null)
                return false;
            else
                return Id.Equals(baseDto.Id);
        }
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
        public T CreateInstance<T>()
        {
            return (T)Activator.CreateInstance(this.GetType());
        }

        //public abstract T Copy<T>() where T : class;

        //protected T BaseCopy<T>(BaseDto dto) where T : class
        //{
        //    dto.Id = this.Id;
        //    dto.CreateDate = this.CreateDate;
        //    dto.CreatedBy = this.CreatedBy;
        //    dto.UpdateDate = this.UpdateDate;
        //    dto.UpdatedBy = this.UpdatedBy;
        //    dto.IsActive = this.IsActive;
        //    return dto as T;
        //}

    }
}
