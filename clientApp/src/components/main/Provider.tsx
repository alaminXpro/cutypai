import { PropsWithChildren } from "react";
import { Provider } from "react-redux";
import { store } from "@/redux-store/store";

export default function MainProvider({ children }: PropsWithChildren) {
    return <Provider store={store}>{children}</Provider>;
}
