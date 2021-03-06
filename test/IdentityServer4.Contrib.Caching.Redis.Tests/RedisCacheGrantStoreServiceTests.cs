﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Contrib.Caching.Redis.Stores;
using IdentityServer4.Contrib.Caching.Redis.Tests.Misc;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace IdentityServer4.Contrib.Caching.Redis.Tests
{
    public class RedisCacheGrantStoreServiceTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture serviceProviderFixture;

        public RedisCacheGrantStoreServiceTests(ServiceProviderFixture serviceProviderFixture)
        {
            this.serviceProviderFixture = serviceProviderFixture;
        }

        [Fact]
        public async Task DistributedCacheGrantStoreService_GetAllAsync_Valid_Subject_Id_Returns_Enumeration()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheService = provider.GetRequiredService<IPersistedGrantStore>();

            var subjectId = Guid.NewGuid();

            var grants = new[]
            {
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString()
                }
            };

            var bytes = GetBytes(Serialize(grants));

            cacheMock.Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            var foundGrants = await cacheService.GetAllAsync(subjectId.ToString());

            cacheMock.Verify(
                cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(foundGrants);
            Assert.Equal(3, foundGrants.Count());
            var foundBytes = GetBytes(Serialize(foundGrants));

            Assert.Equal(bytes, foundBytes);
        }

        [Fact]
        public async Task DistributedCacheGrantStoreService_GetAllAsync_Invalid_Subject_Id_Returns_Empty_Enumeration()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheService = provider.GetRequiredService<IPersistedGrantStore>();

            var subjectId = Guid.NewGuid();

            cacheMock.Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            var foundGrants = await cacheService.GetAllAsync(subjectId.ToString());

            cacheMock.Verify(
                cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            cacheMock.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            Assert.NotNull(foundGrants);
            Assert.Empty(foundGrants);
        }

        [Fact]
        public async Task
            DistributedCacheGrantStoreService_RemoveAllAsync_For_Valid_SubjectId_And_ClientId_Returns_Enumeration()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheService = provider.GetRequiredService<IPersistedGrantStore>();

            var subjectId = Guid.NewGuid();

            var grants = new[]
            {
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "1",
                    Expiration = DateTime.UtcNow.AddDays(1),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "1",
                    Expiration = DateTime.UtcNow.AddDays(1),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "2",
                    Expiration = DateTime.UtcNow.AddDays(1),
                }
            };

            var bytes = GetBytes(Serialize(grants));

            cacheMock.Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            await cacheService.RemoveAllAsync(subjectId.ToString(), "1");

            cacheMock.Verify(
                cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            cacheMock.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(4));
        }

        [Fact]
        public async Task
            DistributedCacheGrantStoreService_RemoveAllAsync_For_Valid_SubjectId_And_ClientId_NoValues_Found()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheService = provider.GetRequiredService<IPersistedGrantStore>();

            var subjectId = Guid.NewGuid();

            var grants = new[]
            {
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "2",
                    Expiration = DateTime.UtcNow.AddDays(1),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "2",
                    Expiration = DateTime.UtcNow.AddDays(1),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "2",
                    Expiration = DateTime.UtcNow.AddDays(1),
                }
            };

            var bytes = GetBytes(Serialize(grants));

            cacheMock.Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            await cacheService.RemoveAllAsync(subjectId.ToString(), "1");

            cacheMock.Verify(
                cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            cacheMock.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task
            DistributedCacheGrantStoreService_RemoveAllAsync_For_Valid_SubjectId_ClientId_Type_Returns_Enumeration()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheService = provider.GetRequiredService<IPersistedGrantStore>();

            var subjectId = Guid.NewGuid();

            var grants = new[]
            {
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "1",
                    Type = SubjectTypes.Global.ToString(),
                    Expiration = DateTime.UtcNow.AddDays(1),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "1",
                    Type = SubjectTypes.Global.ToString(),
                    Expiration = DateTime.UtcNow.AddDays(1),
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "1",
                    Type = SubjectTypes.Global.ToString(),
                    Expiration = DateTime.UtcNow.AddDays(1),
                },
            };

            var bytes = GetBytes(Serialize(grants));

            cacheMock.Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            await cacheService.RemoveAllAsync(subjectId.ToString(), "1", SubjectTypes.Global.ToString());

            cacheMock.Verify(
                cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            cacheMock.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Exactly(6));
        }

        [Fact]
        public async Task
            DistributedCacheGrantStoreService_RemoveAllAsync_For_Valid_SubjectId_ClientId_Type_No_Values_Found()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheService = provider.GetRequiredService<IPersistedGrantStore>();

            var subjectId = Guid.NewGuid();

            var grants = new[]
            {
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "2",
                    Type = SubjectTypes.Global.ToString()
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "2",
                    Type = SubjectTypes.Global.ToString()
                },
                new PersistedGrant
                {
                    SubjectId = subjectId.ToString(),
                    ClientId = "2",
                    Type = SubjectTypes.Global.ToString()
                }
            };

            var bytes = GetBytes(Serialize(grants));

            cacheMock.Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            await cacheService.RemoveAllAsync(subjectId.ToString(), "2", SubjectTypes.Global.ToString());

            cacheMock.Verify(
                cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            cacheMock.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
        }

        [Fact]
        public async Task DistributedCacheGrantStoreService_RemoveAsync_Succeeds()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheService = provider.GetRequiredService<IPersistedGrantStore>();

            await cacheService.RemoveAsync("some-key-that-is-not-relevant");

            cacheMock.Verify(
                cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DistributedCacheGrantStoreService_GetAsync_Valid_Key_Succeeds()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheStoreService = provider.GetRequiredService<IPersistedGrantStore>();

            var key = Guid.NewGuid();

            var grant = new PersistedGrant
            {
                ClientId = "Client1",
                CreationTime = DateTime.UtcNow,
                Expiration = DateTime.UtcNow.AddDays(1),
                Key = key.ToString(),
                SubjectId = Guid.NewGuid().ToString(),
                Type = IdentityServerConstants.PersistedGrantTypes.RefreshToken,
                Data = Encoding.UTF8.GetString(key.ToByteArray())
            };

            var returnBytes = GetBytes(Serialize(grant));

            cacheMock.Setup(cache =>
                    cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnBytes);

            var foundGrant = await cacheStoreService.GetAsync(key.ToString());

            cacheMock.Verify(
                cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(foundGrant);
            var foundBytes = GetBytes(Serialize(foundGrant));
            Assert.Equal(returnBytes, foundBytes);
        }

        [Fact]
        public async Task DistributedCacheGrantStoreService_StoreAsync_Verify_Calls_And_Return_Values_Succeeds()
        {
            var cacheMock = this.serviceProviderFixture.CreateDistributedCacheMock();

            var lockManagerMock = this.serviceProviderFixture.CreateRedisLockManagerMock();

            var provider = this.serviceProviderFixture.BuildMockServiceProvider(cacheMock.Object, lockManagerMock.Object);

            var cacheStoreService = provider.GetRequiredService<IPersistedGrantStore>();

            var subjectId = Guid.NewGuid();

            var grants = new[]
            {
                new PersistedGrant
                {
                    ClientId = "Client1",
                    CreationTime = DateTime.UtcNow,
                    Expiration = DateTime.UtcNow.AddDays(1),
                    Key = subjectId.ToString(),
                    SubjectId = subjectId.ToString(),
                    Type = IdentityServerConstants.PersistedGrantTypes.RefreshToken,
                    Data = Encoding.UTF8.GetString(subjectId.ToByteArray())
                }
            };

            var bytes = GetBytes(Serialize(grants));

            cacheMock.Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(bytes);

            await cacheStoreService.StoreAsync(grants[0]);

            cacheMock.Verify(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

            cacheMock.Verify(cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);

            cacheMock.Verify(cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
        }

        private static string Serialize(object @object) =>
            JsonConvert.SerializeObject(@object, RedisCacheGrantStore.SerializerSettings);

        private static byte[] GetBytes(string value) => Encoding.UTF8.GetBytes(value);
    }
}