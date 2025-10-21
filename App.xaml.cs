using System.Configuration;
using System.Data;
using System.Windows;
using SaveCodeClassfication.Views;
using WpfMessageBox = System.Windows.MessageBox;

namespace SaveCodeClassfication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 전역 예외 처리기 설정
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += App_UnhandledException;
            
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"=== Dispatcher 예외 발생 ===");
            System.Diagnostics.Debug.WriteLine($"예외 타입: {e.Exception.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"예외 메시지: {e.Exception.Message}");
            System.Diagnostics.Debug.WriteLine($"스택 트레이스: {e.Exception.StackTrace}");
            
            WpfMessageBox.Show($"예상치 못한 오류가 발생했습니다:\n{e.Exception.Message}", 
                          "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true; // 애플리케이션 종료 방지
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"=== 전역 예외 발생 ===");
            if (e.ExceptionObject is Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"예외 타입: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"예외 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
            }
            System.Diagnostics.Debug.WriteLine($"종료 중: {e.IsTerminating}");
        }

        /// <summary>
        /// 애플리케이션 시작 이벤트
        /// </summary>
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 애플리케이션 시작 ===");
                
                // 로그인 창 표시
                System.Diagnostics.Debug.WriteLine("로그인 창을 표시하는 중...");
                var loginWindow = new LoginWindow();
                
                var loginDialogResult = loginWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"로그인 창 DialogResult: {loginDialogResult}");
                System.Diagnostics.Debug.WriteLine($"로그인 성공 여부: {loginWindow.IsLoginSuccessful}");
                System.Diagnostics.Debug.WriteLine($"로그인 사용자: {loginWindow.LoggedInUserId}");
                System.Diagnostics.Debug.WriteLine($"관리자 권한: {loginWindow.IsAdminUser}");
                
                if (loginDialogResult == true && loginWindow.IsLoginSuccessful)
                {
                    System.Diagnostics.Debug.WriteLine("로그인 성공 - 메인 창을 여는 중...");
                    
                    try
                    {
                        // 로그인 성공 시 메인 창 열기
                        System.Diagnostics.Debug.WriteLine("MainWindow 인스턴스 생성 시작...");
                        var mainWindow = new MainWindow();
                        System.Diagnostics.Debug.WriteLine("MainWindow 인스턴스 생성 완료");
                        
                        mainWindow.LoggedInUserId = loginWindow.LoggedInUserId;
                        mainWindow.IsAdminUser = loginWindow.IsAdminUser; // 관리자 권한 정보 전달
                        
                        System.Diagnostics.Debug.WriteLine($"메인 창 설정 완료 - 사용자: {mainWindow.LoggedInUserId}, 관리자: {mainWindow.IsAdminUser}");
                        
                        // 메인 창을 애플리케이션의 메인 창으로 설정
                        MainWindow = mainWindow;
                        System.Diagnostics.Debug.WriteLine("MainWindow를 애플리케이션 메인 창으로 설정 완료");
                        
                        // ShutdownMode를 MainWindow가 닫힐 때로 변경
                        ShutdownMode = ShutdownMode.OnMainWindowClose;
                        System.Diagnostics.Debug.WriteLine("ShutdownMode를 OnMainWindowClose로 변경 완료");
                        
                        System.Diagnostics.Debug.WriteLine("메인 창 Show() 호출 중...");
                        
                        // 메인창을 확실히 표시하기 위한 다양한 방법 시도
                        mainWindow.Visibility = Visibility.Visible;
                        mainWindow.WindowState = WindowState.Normal;
                        mainWindow.Show();
                        mainWindow.Activate();
                        mainWindow.Focus();
                        
                        System.Diagnostics.Debug.WriteLine("메인 창 Show() 완료");
                        System.Diagnostics.Debug.WriteLine($"메인창 상태 - Visibility: {mainWindow.Visibility}, WindowState: {mainWindow.WindowState}, IsVisible: {mainWindow.IsVisible}");
                        
                        // 창 활성화
                        mainWindow.Activate();
                        System.Diagnostics.Debug.WriteLine("메인창 Activate() 완료");
                        
                        // 메인창 닫힘 이벤트 처리
                        mainWindow.Closed += (s, args) =>
                        {
                            System.Diagnostics.Debug.WriteLine("메인창이 닫혔습니다. 애플리케이션을 종료합니다.");
                            // OnMainWindowClose 모드에서는 자동으로 종료됨
                        };
                        
                        // 메인창이 예상치 못하게 닫히는 것을 방지
                        mainWindow.Closing += (s, args) =>
                        {
                            System.Diagnostics.Debug.WriteLine("메인창 종료 시도 감지");
                            
                            // 임시로 창 닫기를 취소해서 문제를 파악
                            if (System.Diagnostics.Debugger.IsAttached)
                            {
                                var result = System.Windows.MessageBox.Show(
                                    "메인창이 닫히려고 합니다. 계속 진행하시겠습니까?", 
                                    "디버그 확인", 
                                    MessageBoxButton.YesNo, 
                                    MessageBoxImage.Question);
                                
                                if (result == MessageBoxResult.No)
                                {
                                    args.Cancel = true;
                                    System.Diagnostics.Debug.WriteLine("메인창 닫기가 취소되었습니다.");
                                    return;
                                }
                            }
                        };
                        
                        // 메인창 표시 후 간단한 초기화 작업
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("메인창 후처리 시작");
                            await mainWindow.PostInitializeAsync();
                            System.Diagnostics.Debug.WriteLine("메인창 후처리 완료");
                        }
                        catch (Exception postEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"메인창 후처리 오류: {postEx.Message}");
                            // 후처리 오류는 치명적이지 않으므로 계속 진행
                        }
                        
                        System.Diagnostics.Debug.WriteLine("✅ 메인 창 설정 및 표시 완료");
                        System.Diagnostics.Debug.WriteLine("애플리케이션이 정상적으로 실행 중입니다...");
                    }
                    catch (Exception mainWindowEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"=== 메인창 생성/표시 중 오류 ===");
                        System.Diagnostics.Debug.WriteLine($"오류 타입: {mainWindowEx.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"오류 메시지: {mainWindowEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"스택 트레이스: {mainWindowEx.StackTrace}");
                        
                        WpfMessageBox.Show($"메인 창을 여는 중 오류가 발생했습니다:\n{mainWindowEx.Message}", 
                                      "메인창 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        Shutdown();
                    }
                }
                else
                {
                    // 로그인 실패 또는 취소 시 애플리케이션 종료
                    System.Diagnostics.Debug.WriteLine("로그인 실패 또는 취소 - 애플리케이션을 종료하는 중...");
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 애플리케이션 시작 중 오류 ===");
                System.Diagnostics.Debug.WriteLine($"오류 타입: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");
                
                WpfMessageBox.Show($"애플리케이션 시작 중 오류가 발생했습니다:\n{ex.Message}", 
                              "시작 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
