using Xunit;

// Disable parallel test execution across test classes to avoid race conditions on the static CurrentSession singleton.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
