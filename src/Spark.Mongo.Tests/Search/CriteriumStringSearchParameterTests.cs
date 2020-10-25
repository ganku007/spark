/* 
 * Copyright (c) 2020, Incendi (info@Incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Utility;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Spark.Search;
using Spark.Search.Mongo;
using System.Linq;
using Xunit;

namespace Spark.Mongo.Tests.Search
{
    public partial class CriteriumStringSearchParameterTests
    {
        [Theory]
        [InlineData(
            ResourceType.Subscription, 
            "criteria", 
            "criteria=Observation?patient.identifier=http://somehost.no/fhir/Name%20Hospital|someId",
            "{ \"criteria\" : /^Observation?patient.identifier=http:\\/\\/somehost.no\\/fhir\\/Name%20Hospital|someId/i }")]
        [InlineData(
            ResourceType.Patient, 
            "name",
            "name:missing=true",
            "{ \"$or\" : [{ \"name\" : { \"$exists\" : false } }, { \"name\" : null }] }")]
        [InlineData(
            ResourceType.Patient, 
            "name",
            "name:missing=false",
            "{ \"name\" : { \"$ne\" : null } }")]
        [InlineData(ResourceType.Patient, "name", "name=eve", "{ \"name\" : /^eve/i }")]
        [InlineData(ResourceType.Patient, "name", "name:contains=eve", "{ \"name\" : /.*eve.*/i }")]
        [InlineData(ResourceType.Patient, "name", "name:exact=Eve", "{ \"name\" : \"Eve\" }")]
        [InlineData(ResourceType.Condition, "code", "code:text=headache", "{ \"code.text\" : /headache/i }")]
        [InlineData(ResourceType.Condition, 
            "code", 
            "code:not=headache", 
            "{ \"$or\" : [{ \"code\" : { \"$elemMatch\" : { \"code\" : { \"$exists\" : true, \"$ne\" : \"headache\" } } } }, { \"code\" : { \"$not\" : { \"$type\" : 4 }, \"$exists\" : true }, \"code.code\" : { \"$ne\" : \"headache\" } }, { \"code\" : { \"$type\" : 2, \"$exists\" : true, \"$ne\" : \"headache\" } }] }")]

        public void Can_Build_Filter_String_Search(ResourceType resourceType, string searchParameter, string query, string expected)
        {
            var bsonSerializerRegistry = new BsonSerializerRegistry();
            bsonSerializerRegistry.RegisterSerializationProvider(new BsonSerializationProvider());

            var resourceTypeAsString = resourceType.GetLiteral();
            var criterium = Criterium.Parse(query);
            criterium.SearchParameters.AddRange(ModelInfo.SearchParameters.Where(p => p.Resource == resourceTypeAsString && p.Name == searchParameter));

            var filter = criterium.ToFilter(resourceType.GetLiteral());

            var jsonFilter = filter?.Render(null, bsonSerializerRegistry)?.ToJson();
            Assert.Equal(expected, jsonFilter);
        }
    }
}
