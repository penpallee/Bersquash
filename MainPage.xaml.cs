using Bersquash.Pages;
using Firebase.Firestore;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;

namespace Bersquash
{
    public partial class MainPage : ContentPage
    {
        private static readonly List<string> Nicknames = new List<string>
        {
            "BlueDragon", "RedPhoenix", "GreenTiger", "WhiteTurtle", "BlackLeopard"
        };

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeFirestore();
            await CheckDeviceIdAndAuthenticateAsync();

        }
        private async Task InitializeFirestore()
        {
            var firestore = CrossFirebaseFirestore.Current;
            var settings = new FirestoreSettings(
                host: "firestore.googleapis.com",
            isPersistenceEnabled: true, // 오프라인 지속성 사용
            isSslEnabled: true,        // SSL 사용
            cacheSizeBytes: 1200000
            );

            firestore.Settings = settings;

            // 네트워크 활성화 시도
            try
            {
                await firestore.EnableNetworkAsync();
                await DisplayAlert("Info", "Network enabled", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to enable network: {ex.Message}", "OK");
            }
        }

        private async Task CheckDeviceIdAndAuthenticateAsync()
        {
            // 네트워크 연결 상태 확인
            var current = Connectivity.NetworkAccess;
            if (current != NetworkAccess.Internet)
            {
                await DisplayAlert("Error", "No internet connection. Please check your network settings.", "OK");
                return;
            }

            var deviceId = GetDeviceId();
            var firestore = CrossFirebaseFirestore.Current;
            var userDocumentRef = firestore.GetCollection("users").GetDocument(deviceId);
            try
            {
                var userDocumentSnapshot = await userDocumentRef.GetDocumentSnapshotAsync<DocumentSnapshot>();

                if (userDocumentSnapshot != null)
                {
                    // deviceId가 이미 존재하는 경우, 자동 로그인
                    var nickname = userDocumentSnapshot.Data.GetString("nickname");
                    StatusLabel.Text = $"Welcome back, {nickname}!";
                    await Task.Delay(2000); // 잠시 대기 후 홈 페이지로 이동
                    await Navigation.PushAsync(new AppShell());
                }
                else
                {
                    // deviceId가 없는 경우, 익명 로그인 시도
                    await AnonymousLoginByDeviceAsync();
                }
            }
            catch (FirebaseFirestoreException ex)
            {
                await DisplayAlert("Error", ex + "Failed to get document because the client is offline.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to get document: {ex.Message}", "OK");
            }
        }

        private async Task AnonymousLoginByDeviceAsync()
        {
            try
            {
                var auth = CrossFirebaseAuth.Current;
                var result = await auth.SignInAnonymouslyAsync();
                var user = result;

                var nickname = await AssignUniqueNickname(user.Uid);
                await DisplayAlert("Success", $"Logged in as: {user.Uid}\nNickname: {nickname}", "OK");

                // 로그인 성공 후 deviceId를 Firestore에 저장
                var deviceId = GetDeviceId();
                var firestore = CrossFirebaseFirestore.Current;
                var userDocumentRef = firestore.GetCollection("users").GetDocument(deviceId);
                await userDocumentRef.SetDataAsync(new Dictionary<string, object>
                {
                    { "nickname", nickname },
                    { "uid", user.Uid }
                });

                // 로그인 성공 후 Home 페이지로 이동
                await Navigation.PushAsync(new AppShell());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to login: {ex.Message}", "OK");
            }
        }

        private async Task<string> AssignUniqueNickname(string userId)
        {
            var firestore = CrossFirebaseFirestore.Current;
            var usersCollection = firestore.GetCollection("users");

            string nickname;
            bool isUnique;

            do
            {
                var random = new Random();
                nickname = Nicknames[random.Next(Nicknames.Count)];

                var querySnapshot = await usersCollection
                    .WhereEqualsTo("nickname", nickname)
                    .GetDocumentsAsync<QuerySnapshot>();

                isUnique = querySnapshot.Documents.Count() == 0;
            }
            while (!isUnique);

            return nickname;
        }

        private string GetDeviceId()
        {
            var deviceId = Xamarin.Essentials.Preferences.Get("device_id", string.Empty);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString();
                Xamarin.Essentials.Preferences.Set("device_id", deviceId);
            }
            return deviceId;
        }
    }

}
