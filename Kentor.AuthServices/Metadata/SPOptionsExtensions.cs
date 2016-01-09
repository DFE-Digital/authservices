﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Kentor.AuthServices.Configuration;
using Kentor.AuthServices.WebSso;
using System.IdentityModel.Tokens;

namespace Kentor.AuthServices.Metadata
{
    static class SPOptionsExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public static ExtendedEntityDescriptor CreateMetadata(this ISPOptions spOptions, AuthServicesUrls urls)
        {
            var ed = new ExtendedEntityDescriptor
            {
                EntityId = spOptions.EntityId,
                Organization = spOptions.Organization,
                CacheDuration = spOptions.MetadataCacheDuration
            };

            foreach (var contact in spOptions.Contacts)
            {
                ed.Contacts.Add(contact);
            }

            var spsso = new ExtendedServiceProviderSingleSignOnDescriptor();

            spsso.ProtocolsSupported.Add(new Uri("urn:oasis:names:tc:SAML:2.0:protocol"));

            spsso.AssertionConsumerServices.Add(0, new IndexedProtocolEndpoint()
            {
                Index = 0,
                IsDefault = true,
                Binding = Saml2Binding.HttpPostUri,
                Location = urls.AssertionConsumerServiceUrl
            });

            foreach(var attributeService in spOptions.AttributeConsumingServices)
            {
                spsso.AttributeConsumingServices.Add(attributeService);
            }

            if (spOptions.ServiceCertificates != null)
            {
                //TODO: filter out the encryption = current
                foreach (var serviceCert in spOptions.ServiceCertificates)
                {
                    using (var securityToken = new X509SecurityToken(serviceCert.Certificate))
                    {
                        //TODO: change the publish type based on #355 rules
                        spsso.Keys.Add(
                            new KeyDescriptor
                            {
                                Use = (KeyType)(byte)serviceCert.Use,
                                KeyInfo = new SecurityKeyIdentifier(securityToken.CreateKeyIdentifierClause<X509RawDataKeyIdentifierClause>())
                            }
                        );
                    }
                }
            }

            if (spOptions.DiscoveryServiceUrl != null
                && !string.IsNullOrEmpty(spOptions.DiscoveryServiceUrl.OriginalString))
            {
                spsso.Extensions.DiscoveryResponse = new IndexedProtocolEndpoint
                {
                    Binding = Saml2Binding.DiscoveryResponseUri,
                    Index = 0,
                    IsDefault = true,
                    Location = urls.SignInUrl
                };
            }

            ed.RoleDescriptors.Add(spsso);

            return ed;
        }
    }
}
