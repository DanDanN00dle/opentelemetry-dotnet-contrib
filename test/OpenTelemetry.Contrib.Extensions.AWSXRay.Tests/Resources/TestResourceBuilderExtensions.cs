// <copyright file="TestResourceBuilderExtensions.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Linq;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Resources;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.Contrib.Extensions.AWSXRay.Tests.Resources
{
    public class TestResourceBuilderExtensions
    {
        [Fact]
        public void TestAddDetector()
        {
            Environment.SetEnvironmentVariable("AWS_REGION", "us-east-1");
            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME", "testfunction");
            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_VERSION", "latest");

            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                                          .SetResourceBuilder(ResourceBuilder
                                          .CreateDefault()
                                          .AddDetector(new AWSLambdaResourceDetector())) // use lambda resource detector here as it doesn't require sending identical http request to aws endpoint
                                          .Build();

            var resource = tracerProvider.GetResource();
            var resourceAttributes = resource.Attributes.ToDictionary(x => x.Key, x => x.Value);

            Assert.Equal("aws", resourceAttributes[AWSSemanticConventions.AttributeCloudProvider]);
            Assert.Equal("aws_lambda", resourceAttributes[AWSSemanticConventions.AttributeCloudPlatform]);
            Assert.Equal("us-east-1", resourceAttributes[AWSSemanticConventions.AttributeCloudRegion]);
            Assert.Equal("testfunction", resourceAttributes[AWSSemanticConventions.AttributeFaasName]);
            Assert.Equal("latest", resourceAttributes[AWSSemanticConventions.AttributeFaasVersion]);

            Environment.SetEnvironmentVariable("AWS_REGION", null);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME", null);
            Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_VERSION", null);
        }
    }
}
