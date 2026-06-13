import Navbar from '@/components/layout/Navbar'
import StatusBar from '@/components/layout/StatusBar'
import SplitView from '@/components/layout/SplitView'
import Footer from '@/components/layout/Footer'

function AppShell() {
  return (
    <div className="flex h-full flex-col overflow-hidden">
      <Navbar />
      <StatusBar />
      <SplitView />
      <Footer />
    </div>
  )
}

export default AppShell
