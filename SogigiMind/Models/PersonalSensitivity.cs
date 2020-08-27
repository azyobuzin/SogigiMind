using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace SogigiMind.Models
{
    public class PersonalSensitivity
    {
#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        [Required]
        public string User { get; set; }

        [Required]
        public string Url { get; set; }

        public bool Sensitive { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

#pragma warning restore CS8618

        public static Task CreateIndexesAsync(IMongoCollection<PersonalSensitivity> collection)
        {
            return collection.Indexes
                .CreateOneAsync(new CreateIndexModel<PersonalSensitivity>(
                    Builders<PersonalSensitivity>.IndexKeys
                        .Ascending(x => x.User)
                        .Ascending(x => x.Url),
                    new CreateIndexOptions() { Unique = true }));
        }
    }
}
