using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Docs.Documents;
using Volo.Docs.HtmlConverting;
using Volo.Docs.Models;
using Volo.Docs.Projects;

namespace Volo.Docs.Pages.Documents.Project
{
    public class IndexModel : AbpPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string ProjectName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Version { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string DocumentName { get; set; }

        public ProjectDto Project { get; set; }

        public string DocumentNameWithExtension { get; private set; }

        public DocumentWithDetailsDto Document { get; private set; }

        public List<VersionInfo> Versions { get; private set; }

        public List<SelectListItem> VersionSelectItems { get; private set; }

        public NavigationWithDetailsDto Navigation { get; private set; }

        public VersionInfo LatestVersionInfo { get; private set; }

        private readonly IDocumentAppService _documentAppService;
        private readonly IDocumentToHtmlConverterFactory _documentToHtmlConverterFactory;
        private readonly IProjectAppService _projectAppService;

        public IndexModel(
            IDocumentAppService documentAppService, 
            IDocumentToHtmlConverterFactory documentToHtmlConverterFactory, 
            IProjectAppService projectAppService)
        {
            _documentAppService = documentAppService;
            _documentToHtmlConverterFactory = documentToHtmlConverterFactory;
            _projectAppService = projectAppService;
        }

        public async Task OnGetAsync()
        {
            Project = await _projectAppService.GetByShortNameAsync(ProjectName);

            SetDocumentNames();
            await SetVersionAsync();
            await SetDocumentAsync();
            await SetNavigationAsync();
        }

        private void SetDocumentNames()
        {
            if (DocumentName.IsNullOrWhiteSpace())
            {
                DocumentName = Project.DefaultDocumentName;
            }

            DocumentNameWithExtension = DocumentName + "." + Project.Format;
        }

        private async Task SetVersionAsync()
        {
            var versionInfoDtos = await _projectAppService.GetVersionsAsync(Project.Id);

            Versions = versionInfoDtos.Select(v => new VersionInfo(v.DisplayName, v.Name)).ToList();

            LatestVersionInfo = GetLatestVersion();

            if (string.Equals(Version, DocsAppConsts.Latest, StringComparison.OrdinalIgnoreCase))
            {
                LatestVersionInfo.IsSelected = true;
                Version = LatestVersionInfo.Version;
            }
            else
            {
                var versionFromUrl = Versions.FirstOrDefault(v => v.Version == Version);
                if (versionFromUrl != null)
                {
                    versionFromUrl.IsSelected = true;
                    Version = versionFromUrl.Version;
                }
                else
                {
                    Versions.First().IsSelected = true;
                    Version = Versions.First().Version;
                }
            }

            VersionSelectItems = Versions.Select(v => new SelectListItem
            {
                Text = v.DisplayText,
                Value = CreateLink(LatestVersionInfo, v.Version, DocumentName),
                Selected = v.IsSelected
            }).ToList();
        }

        private async Task SetNavigationAsync()
        {
            try
            {
                var document = await _documentAppService.GetNavigationDocumentAsync(
                    new GetNavigationDocumentInput
                    {
                        ProjectId = Project.Id,
                        Version = Version
                    }
                );

                Navigation = ObjectMapper.Map<DocumentWithDetailsDto, NavigationWithDetailsDto>(document);
            }
            catch (DocumentNotFoundException) //TODO: What if called on a remote service which may return 404
            {
                return;
            }

            Navigation.ConvertItems();
        }



        public string CreateLink(VersionInfo latestVersion, string version, string documentName = null)
        {
            if (latestVersion.Version == version)
            {
                version = DocsAppConsts.Latest;
            }

            var link = "/documents/" + ProjectName + "/" + version;

            if (documentName != null)
            {
                link += "/" + DocumentName;
            }

            return link;
        }

        private VersionInfo GetLatestVersion()
        {
            var latestVersion = Versions.First();

            latestVersion.DisplayText = $"{latestVersion.DisplayText} ({DocsAppConsts.Latest})";
            latestVersion.Version = latestVersion.Version;

            return latestVersion;
        }

        public string GetSpecificVersionOrLatest()
        {
            if (Document?.Version == null)
            {
                return DocsAppConsts.Latest;
            }

            return Document.Version == LatestVersionInfo.Version ?
                DocsAppConsts.Latest :
                Document.Version;
        }

        private async Task SetDocumentAsync()
        {
            try
            {
                if (DocumentNameWithExtension.IsNullOrWhiteSpace())
                {
                    Document = await _documentAppService.GetDefaultAsync(
                        new GetDefaultDocumentInput
                        {
                            ProjectId = Project.Id,
                            Version = Version
                        }
                    );
                }
                else
                {
                    Document = await _documentAppService.GetAsync(
                        new GetDocumentInput
                        {
                            ProjectId = Project.Id,
                            Name = DocumentNameWithExtension,
                            Version = Version
                        }
                    );
                }
            }
            catch (DocumentNotFoundException)
            {
                return;
            }
           
            var converter = _documentToHtmlConverterFactory.Create(Document.Format ?? Project.Format);

            var content = converter.NormalizeLinks(Document.Content, Document.Project.ShortName, GetSpecificVersionOrLatest(), Document.LocalDirectory);
            content = converter.Convert(content);

            content = HtmlNormalizer.ReplaceImageSources(content, Document.RawRootUrl, Document.LocalDirectory);
            content = HtmlNormalizer.ReplaceCodeBlocksLanguage(content, "language-C#", "language-csharp"); //todo find a way to make it on client in prismJS configuration (eg: map C# => csharp)

            Document.Content = content;
        }

    }
}