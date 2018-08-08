using System.Collections.Generic;
using System.Data.SqlClient;
using DynamicDbUtil;

namespace ConsoleExample
{
    class Program
    {
        static void Main(string[] args)
        {

            // Examples
            using (SqlConnection conn = new SqlConnection(@"Data Source=.\SQLEXPRESS; Initial Catalog=EXA;Integrated Security=True;Pooling=True"))
            {
                dynamic x = Db.List(conn, "select Id,StringVar,IntVar,DateTimeVar from Example");
                dynamic x21 = Db.List(conn, "select Id,StringVar,IntVar,DateTimeVar from Example where Id={0}", 1);

                ExampleDto y = Db.Map<ExampleDto>(x);


                ExampleDto xy = Db.Get<ExampleDto>(conn, @"
                SELECT 
                       Ad [Adi]
                      ,Soyad [Soyadi]
                      ,KullaniciAd [UserName]
                      ,Sifre [Password]
                      ,kd.AnneAdi [KullaniciDetay.AnneAdi]                   
                  FROM campus.dbo.Kullanici k
                  join dbo.KullaniciDetay kd on k.No=kd.No
                  where k.No={0}
                ", 124);



                dynamic x1 = Db.Get(conn, @"
                SELECT 
                    Ad [Adi]
                    ,Soyad [Soyadi]
                    ,KullaniciAd [UserName]
                    ,Sifre [Password]
                    ,kd.AnneAdi [KullaniciDetay.AnneAdi]                   
                FROM campus.dbo.Kullanici k
                join dbo.KullaniciDetay kd on k.No=kd.No
                where k.No={0}
                ", 124);


                ExampleDto y1 = Db.Map<ExampleDto>(x);


                UserDto x2 = Db.Get<UserDto>(conn, @"
                SELECT                     
                    KullaniciAd [UserName]
                    ,Sifre [Password]
                    ,1 [OldPassword]   
                    ,getdate() [DtLastLogin]          
                    ,2 [TotalLogin]       
                    ,getdate() [DtLastLogin]        
                    ,1 [PasswordTry]     
                    ,1 [ActivationCode]     
                    ,1 [Theme]         
                    ,1 [IsUserActive]
                    ,No [Id]
                    ,getdate() [DtCreated] 
                    ,1 [CreatedBy] 
                    ,getdate() [DtUpdated] 
                    ,1 [UpdatedBy] 
                    ,1 [IsActive]  
                FROM campus.dbo.Kullanici k               
                where k.No={0}
                ", 124);



                List<UserDto> x3 = Db.List<UserDto>(conn, @"
                SELECT                     
                    KullaniciAd [UserName]
                    ,Sifre [Password]
                    ,1 [OldPassword]   
                    ,getdate() [DtLastLogin]          
                    ,2 [TotalLogin]       
                    ,getdate() [DtLastLogin]        
                    ,1 [PasswordTry]     
                    ,1 [ActivationCode]     
                    ,1 [Theme]         
                    ,1 [IsUserActive]
                    ,No [Id]
                    ,getdate() [DtCreated] 
                    ,1 [CreatedBy] 
                    ,getdate() [DtUpdated] 
                    ,1 [UpdatedBy] 
                    ,1 [IsActive]  
                FROM campus.dbo.Kullanici k               
                where k.No<{0}
                ", 124);
            }
        }
    }
}
