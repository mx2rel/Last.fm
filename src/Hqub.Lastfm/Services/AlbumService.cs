﻿namespace Hqub.Lastfm.Services
{
    using Hqub.Lastfm.Entities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class AlbumService : IAlbumService
    {
        private readonly LastfmClient client;

        public AlbumService(LastfmClient client)
        {
            this.client = client;
        }

        /// <inheritdoc />
        public async Task<PagedResponse<Album>> SearchAsync(string album, int page = 1, int limit = 30)
        {
            var request = client.CreateRequest("album", "search");

            request.Parameters["album"] = album;

            request.SetPagination(limit, 30, page, 1);

            var doc = await request.GetAsync();

            var s = ResponseParser.Default;

            var response = new PagedResponse<Album>();

            response.items = s.ReadObjects<Album>(doc, "/lfm/results/albummatches/album");
            response.PageInfo = s.ParseOpenSearch(doc.Root.Element("results"));

            return response;
        }

        /// <inheritdoc />
        public async Task<Album> GetInfoAsync(string album, string artist, string lang = null, bool autocorrect = true)
        {
            var request = client.CreateRequest("album", "getInfo");

            SetParameters(request, album, artist, null, autocorrect);

            if (!string.IsNullOrEmpty(lang))
            {
                request.Parameters["lang"] = lang;
            }

            var doc = await request.GetAsync();

            var s = ResponseParser.Default;

            return s.ReadObject<Album>(doc.Root.Element("album"));
        }

        /// <inheritdoc />
        public async Task<List<Tag>> GetTopTagsAsync(string album, string artist, bool autocorrect = true)
        {
            var request = client.CreateRequest("album", "getTopTags");

            SetParameters(request, album, artist, null, autocorrect);

            var doc = await request.GetAsync();

            var s = ResponseParser.Default;

            return s.ReadObjects<Tag>(doc, "/lfm/toptags/tag");
        }

        #region Authenticated

        /// <inheritdoc />
        public async Task<bool> AddTagsAsync(string album, string artist, IEnumerable<string> tags)
        {
            var request = client.CreateRequest("album", "addTags");

            request.EnsureAuthenticated();

            request.Parameters["tags"] = string.Join(",", tags.Take(10));

            SetParameters(request, album, artist, null, false);

            var doc = await request.PostAsync();

            var s = ResponseParser.Default;

            return s.IsStatusOK(doc.Root);
        }

        /// <inheritdoc />
        public async Task<bool> RemoveTagAsync(string album, string artist, string tag)
        {
            var request = client.CreateRequest("album", "removeTag");

            request.EnsureAuthenticated();

            request.Parameters["tag"] = tag;

            SetParameters(request, album, artist, null, false);

            var doc = await request.PostAsync();

            var s = ResponseParser.Default;

            return s.IsStatusOK(doc.Root);
        }

        #endregion

        private void SetParameters(Request request, string album, string artist, string mbid, bool autocorrect = false)
        {
            if (string.IsNullOrEmpty(artist))
            {
                throw new ArgumentNullException("artist");
            }

            if (string.IsNullOrEmpty(album))
            {
                throw new ArgumentNullException("album");
            }

            request.Parameters["artist"] = artist;
            request.Parameters["album"] = album;

            if (autocorrect)
            {
                request.Parameters["autocorrect"] = "1";
            }

            if (!string.IsNullOrEmpty(mbid))
            {
                request.Parameters["mbid"] = mbid;
            }
        }
    }
}
