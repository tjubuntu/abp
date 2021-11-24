using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Blogging.Blogs;
using Volo.Blogging.Posts;

namespace Volo.BloggingTestApp
{
    public class BloggingDemoDataSeeder : IDataSeedContributor, ITransientDependency
    {
        protected IBlogRepository blogRepository;
        protected IPostRepository postRepository;

        public BloggingDemoDataSeeder(IBlogRepository blogRepository, IPostRepository postRepository)
        {
            this.blogRepository = blogRepository;
            this.postRepository = postRepository;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            var blog = await blogRepository.InsertAsync(
                new Blog(
                    Guid.NewGuid(),
                    "default",
                    "default")
                );

            var post = await postRepository.InsertAsync(
                 new Post(
                     Guid.NewGuid(),
                     blog.Id,
                     "Hello World!",
                     "https://dummyimage.com/600x400/000/fff",
                     "hello-world"
                     ));

            post.Content = "#Hello World \nThis is a sample post.";
        }
    }
}
