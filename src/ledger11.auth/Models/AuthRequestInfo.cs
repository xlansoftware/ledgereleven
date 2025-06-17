    public record AuthRequestInfo
    {
        public string UserId { get; set; } = default!;
        public string ClientId { get; init; } = default!;
        public string RedirectUri { get; init; } = default!;
        public string Username { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string State { get; init; } = default!;
        public string Nonce { get; init; } = default!;
    }
