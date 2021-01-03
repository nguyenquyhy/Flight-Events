import * as React from 'react';
import styled from 'styled-components';
import { Badge } from 'reactstrap';

export default () => {
    const [profile, setProfile] = React.useState<UserProfile | null>(null);
    React.useEffect(() => {
        if (!profile) {
            const f = async () => {
                const response = await fetch('/api/Users/Me');
                if (response.ok) {
                    const data = await response.json() as UserProfile;
                    setProfile(data);
                }
            }

            f();
        }
    });

    if (!profile) {
        return null;
    }

    return <StyledWrapper>
        <svg xmlns="http://www.w3.org/2000/svg" height="38" width="38" viewBox="0 0 144 144">
            <g>
                <ellipse cy="72" cx="72" ry="72" rx="72" fill="#FFFFFF" />
                <path id="path1" transform="rotate(0,72,72) translate(32,32.0000143051147) scale(2.5,2.5)  " fill="#000000" d="M16,2.0050001C12.416,2.0050001 9.5,5.0289993 9.5,8.7459979 9.5,12.462997 12.416,15.487996 16,15.487997 19.584,15.487996 22.5,12.462997 22.5,8.7459979 22.5,5.0289993 19.584,2.0050001 16,2.0050001z M16,0C20.687,0 24.5,3.9239988 24.5,8.7459979 24.5,11.760372 23.010548,14.423184 20.74917,15.996397L20.493732,16.165044 20.752514,16.244553C27.261414,18.335448,32,24.603727,32,31.991016L30,31.999989C30,24.00401 23.719971,17.505989 16,17.505989 8.2800293,17.505989 2,24.00401 2,31.991016L0,31.999989 0,31.991016C0,24.603727,4.7385874,18.335448,11.247486,16.244553L11.506267,16.165044 11.25083,15.996397C8.9894533,14.423184 7.5,11.760372 7.5,8.7459979 7.5,3.9239988 11.313,0 16,0z" />
            </g>
        </svg>

        <StyledInfo>
            <div>{profile.name}</div>
            <Badge>{profile.role}</Badge>
            <a href="/Logout">Logout</a>
        </StyledInfo>
    </StyledWrapper>
}

const StyledInfo = styled.div`
position: absolute;
top: 38px;
right: 0;
z-index: 10000;
width: 200px;
background: rgba(255, 255, 255, 0.9);
border-radius: 0.25rem;
padding: 5px;
text-align: right;

a {
    display: block;
}

display: none;
`

const StyledWrapper = styled.div`
position: absolute;
top: 55px;
right: 10px;
z-index: 10000;
width: 38px;
height: 38px;
background: rgba(255, 255, 255, 0.7);
border-radius: 0.25rem;

:hover ${StyledInfo} {
    display: block;
}
`

interface UserProfile {
    id: string;
    name: string;
    username: string;
    role: string;
}